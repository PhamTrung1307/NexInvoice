# NexInvoice

NexInvoice is a fullstack freelance invoice and project management system. It is built as a Clean Architecture modular monolith with a React/Vite frontend, designed to be practical for demos, code review, and junior .NET portfolio interviews.

## Tech Stack

- Backend: ASP.NET Core Web API, .NET 8, Entity Framework Core, SQL Server
- Infrastructure: Redis cache, Hangfire, SignalR, QuestPDF, MailKit, Serilog, Swagger/OpenAPI
- Frontend: React, Vite, React Router, TanStack React Query, Axios, Tailwind CSS
- Testing: xUnit, EF Core InMemory, ASP.NET Core WebApplicationFactory integration tests
- Deploy: Docker, Docker Compose, Nginx reverse proxy

## Main Modules

- Authentication with JWT access tokens, refresh token rotation, logout, BCrypt password hashing
- Role and permission authorization for admin/freelancer/client access
- Clients/customers CRUD
- Projects CRUD and status tracking
- Tasks by project with create/update/status actions and attachment upload support
- Invoices with line items, send/cancel/mark-paid actions, and PDF generation
- Payments with proof upload, admin confirm/reject workflow, and invoice status updates
- Contracts with CRUD, upload/download, approve/reject workflow
- Notifications, SignalR realtime hub, dashboard metrics, and reports
- Settings for company profile and system preferences

## Project Structure

```text
src/
  NexInvoice.API/             Controllers, middleware, auth policies, hubs, startup
  NexInvoice.Application/     DTOs, interfaces, exceptions, settings, permissions
  NexInvoice.Domain/          Entities, enums, base domain classes
  NexInvoice.Infrastructure/  EF Core, services, identity, seed data, background jobs
tests/
  NexInvoice.UnitTests/       Unit and API integration tests
nexinvoice-web/               React + Vite frontend
docker-compose.yml
docker-compose.prod.yml
DEPLOYMENT_VPS.md
```

## Local Run

Backend:

```powershell
dotnet restore .\NexInvoice.slnx
dotnet build .\NexInvoice.slnx
dotnet run --project .\src\NexInvoice.API\NexInvoice.API.csproj
```

Frontend:

```powershell
cd nexinvoice-web
npm install
npm run dev
```

Default frontend API base URL is `/api/v1`. Set `VITE_API_BASE_URL` when the API is hosted elsewhere.

## Docker Run

Development stack:

```powershell
docker compose up --build
```

Production-style stack:

```powershell
docker compose -f docker-compose.prod.yml up -d --build
```

Useful URLs:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Hangfire: `http://localhost:8080/hangfire`
- SignalR hub: `/hubs/notifications`

## Demo Account

Demo users are seeded only when `Database:SeedDemoUsers` or `SEED_DEMO_USERS=true` is enabled.

```text
Email:    admin@nexinvoice.com
Password: Admin@123
Role:     Admin
```

## Main End-to-End Flows

1. Client -> Project -> Task
   - Create a client.
   - Create a project for that client.
   - Create tasks inside the project and update task status.

2. Invoice -> Items -> Send/Cancel/Mark Paid/PDF
   - Create an invoice from a project.
   - Add invoice item data.
   - Send, cancel, mark paid, or download PDF.

3. Payment -> Upload Proof -> Confirm/Reject
   - Create a payment for an invoice.
   - Upload proof file.
   - Admin confirms or rejects the payment.
   - Confirmed payments update invoice status.

## API Docs

Swagger is available in development, or when `Swagger:Enabled=true`:

```text
http://localhost:8080/swagger
```

Main routes use `/api/v1/...`, for example:

- `POST /api/v1/auth/login`
- `GET /api/v1/clients`
- `GET /api/v1/projects`
- `GET /api/v1/projects/{projectId}/tasks`
- `GET /api/v1/invoices`
- `GET /api/v1/invoices/{id}/pdf`
- `POST /api/v1/payments`
- `PATCH /api/v1/payments/{id}/confirm`

## Tests

```powershell
dotnet test .\NexInvoice.slnx
```

Current coverage includes core business-rule tests and API integration tests for:

- Auth register/login
- Clients CRUD
- Projects CRUD
- Invoices create/mark-paid/cancel
- Payments create/confirm/reject

Frontend build check:

```powershell
cd nexinvoice-web
npm run build
```

## CI

GitHub Actions workflow is defined at `.github/workflows/ci.yml` and runs on push/pull request to `main`:

- Restore backend
- Build backend
- Test backend
- Install frontend dependencies
- Build frontend

## Security Notes

- JWT secrets and database passwords must be provided through environment variables or deployment secrets in production.
- Uploads are limited to 10MB and restricted to whitelisted extensions/content types.
- Stored upload filenames are randomized GUID names.
- Permission policies are applied to business controllers including contracts, reports, and settings.

## Deployment

See `DEPLOYMENT_VPS.md` for Docker Compose based VPS deployment. Automatic deploy is intentionally not enabled because server secrets are not part of the repository.
