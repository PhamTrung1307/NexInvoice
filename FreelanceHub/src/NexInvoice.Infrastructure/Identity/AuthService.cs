using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Settings;
using NexInvoice.Application.Features.Auth;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace NexInvoice.Infrastructure.Identity;

internal sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;

    public AuthService(AppDbContext dbContext, IOptions<JwtSettings> jwtOptions)
    {
        _dbContext = dbContext;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRegisterRequest(request);

        var email = NormalizeEmail(request.Email);
        var emailExists = await _dbContext.AppUsers
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new BadRequestException("Email đã được sử dụng.");
        }

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim(),
            IsActive = true
        };

        _dbContext.AppUsers.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AssignDefaultClientRoleAsync(user.Id, cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException("Email và mật khẩu là bắt buộc.");
        }

        var email = NormalizeEmail(request.Email);
        var user = await GetUserWithRolesAsync(email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("Tài khoản đã bị khóa.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token là bắt buộc.");
        }

        var refreshToken = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user!.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            throw new UnauthorizedException("Refresh token không hợp lệ.");
        }

        if (refreshToken.IsRevoked || refreshToken.RevokedAt is not null)
        {
            throw new UnauthorizedException("Refresh token đã bị thu hồi.");
        }

        if (refreshToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedException("Refresh token đã hết hạn.");
        }

        if (refreshToken.User is null || !refreshToken.User.IsActive)
        {
            throw new UnauthorizedException("Tài khoản không hợp lệ.");
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTimeOffset.UtcNow;

        return await CreateAuthResponseAsync(refreshToken.User, cancellationToken);
    }

    public async Task LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token là bắt buộc.");
        }

        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            throw new NotFoundException("Không tìm thấy refresh token.");
        }

        if (!refreshToken.IsRevoked)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.AppUsers
            .Include(currentUser => currentUser.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role!.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(currentUser => currentUser.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Không tìm thấy người dùng.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("Tài khoản đã bị khóa.");
        }

        var roles = await GetRolesAsync(user, cancellationToken);
        var permissions = await GetPermissionsAsync(user, cancellationToken);

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            roles,
            permissions);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var roles = await GetRolesAsync(user, cancellationToken);
        var permissions = await GetPermissionsAsync(user, cancellationToken);
        var accessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var accessToken = CreateAccessToken(user, roles, permissions, accessTokenExpiresAt);
        var refreshToken = CreateRefreshToken(refreshTokenExpiresAt, user.Id);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Email,
            user.FullName,
            roles,
            accessToken,
            accessTokenExpiresAt,
            refreshToken.Token,
            refreshTokenExpiresAt);
    }

    private string CreateAccessToken(
        AppUser user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        DateTimeOffset expiresAt)
    {
        var secret = GetJwtSecret();

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret is not configured.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new("UserId", user.Id.ToString()),
            new("Email", user.Email),
            new("FullName", user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(roles.Select(role => new Claim("Roles", role)));
        claims.AddRange(permissions.Select(permission => new Claim("Permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static RefreshToken CreateRefreshToken(DateTimeOffset expiresAt, Guid userId)
    {
        return new RefreshToken
        {
            Token = GenerateSecureToken(),
            ExpiresAt = expiresAt,
            UserId = userId
        };
    }

    private async Task<AppUser?> GetUserWithRolesAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AppUsers
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role!.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    private async Task<IReadOnlyCollection<string>> GetRolesAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        if (user.UserRoles.Count == 0)
        {
            await _dbContext.Entry(user)
                .Collection(currentUser => currentUser.UserRoles)
                .Query()
                .Include(userRole => userRole.Role)
                .ThenInclude(role => role!.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
                .LoadAsync(cancellationToken);
        }

        return user.UserRoles
            .Where(userRole => userRole.Role is not null)
            .Select(userRole => userRole.Role!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        if (user.UserRoles.Count == 0)
        {
            await _dbContext.Entry(user)
                .Collection(currentUser => currentUser.UserRoles)
                .Query()
                .Include(userRole => userRole.Role)
                .ThenInclude(role => role!.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
                .LoadAsync(cancellationToken);
        }

        return user.UserRoles
            .Where(userRole => userRole.Role is not null)
            .SelectMany(userRole => userRole.Role!.RolePermissions)
            .Where(rolePermission => rolePermission.Permission is not null)
            .Select(rolePermission => rolePermission.Permission!.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task AssignDefaultClientRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var clientRole = await _dbContext.Roles
            .FirstOrDefaultAsync(role => role.Name == AppRoles.Client, cancellationToken);

        if (clientRole is null)
        {
            return;
        }

        var exists = await _dbContext.UserRoles
            .AnyAsync(userRole =>
                userRole.UserId == userId
                && userRole.RoleId == clientRole.Id,
                cancellationToken);

        if (!exists)
        {
            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = clientRole.Id
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private string GetJwtSecret()
    {
        return string.IsNullOrWhiteSpace(_jwtSettings.SecretKey)
            ? _jwtSettings.Secret
            : _jwtSettings.SecretKey;
    }

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors.Add("Họ tên là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Mật khẩu là bắt buộc.");
        }
        else if (request.Password.Length < 8)
        {
            errors.Add("Mật khẩu phải có ít nhất 8 ký tự.");
        }

        if (errors.Count > 0)
        {
            throw new BadRequestException("Dữ liệu đăng ký không hợp lệ.", errors);
        }
    }
}
