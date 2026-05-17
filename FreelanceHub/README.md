# NexInvoice

NexInvoice is a modular monolith for freelance invoice and project management. It helps freelancers manage customers, projects, work items, invoices, payments, notifications, dashboard metrics, and reminder jobs from one Clean Architecture backend.

The project is intentionally kept as a strong junior-to-mid .NET portfolio project: production-minded structure, clear boundaries, practical infrastructure, and no premature microservices.

Author: Pham Nguyen Anh Trung
Display name: Trung

## Features

- JWT authentication with refresh token rotation and logout.
- BCrypt password hashing.
- Role-based and permission-based authorization.
- Customer, project, work item, invoice, payment, notification, and dashboard modules.
- Invoice PDF generation with QuestPDF.
- Payment proof upload with validation.
- Real-time notifications with SignalR.
- Background invoice reminder jobs with Hangfire.
- Redis caching for dashboard summaries.
- SQL Server persistence with EF Core Fluent API.
- Docker Compose setup for API, SQL Server, and Redis.
- xUnit tests for core business rules.

## Tech Stack

- ASP.NET Core Web API
- .NET 10
- Entity Framework Core
- SQL Server
- Redis
- SignalR
- Hangfire
- MailKit
- QuestPDF
- Bogus
- Swagger / OpenAPI
- xUnit
- Docker / Docker Compose
- React + Vite frontend with Vietnamese UI

## Architecture

NexInvoice follows Clean Architecture and modular monolith principles.

```text
NexInvoice.API -> NexInvoice.Application -> NexInvoice.Domain
NexInvoice.API -> NexInvoice.Infrastructure -> NexInvoice.Application -> NexInvoice.Domain
```

### Layer Responsibilities

- `NexInvoice.Domain`: Entities, enums, common domain primitives, domain events, and value object placeholders.
- `NexInvoice.Application`: Feature-based DTOs/contracts, interfaces, common models, settings, exceptions, and authorization constants.
- `NexInvoice.Infrastructure`: EF Core, repositories, services, identity implementations, background jobs, caching, storage placeholders, and dependency injection.
- `NexInvoice.API`: Controllers, middlewares, filters, extensions, authorization handlers, SignalR hubs, and API startup configuration.

## Folder Structure

```text
NexInvoice/
├── src/
│   ├── NexInvoice.API/
│   │   ├── Authorization/
│   │   ├── Controllers/
│   │   ├── Extensions/
│   │   ├── Filters/
│   │   ├── Hubs/
│   │   ├── Middlewares/
│   │   ├── Services/
│   │   └── Program.cs
│   ├── NexInvoice.Application/
│   │   ├── Common/
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   ├── Customers/
│   │   │   ├── Dashboard/
│   │   │   ├── Invoices/
│   │   │   ├── Payments/
│   │   │   ├── Projects/
│   │   │   └── WorkItems/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   └── UseCases/
│   ├── NexInvoice.Domain/
│   │   ├── Common/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Events/
│   │   └── ValueObjects/
│   └── NexInvoice.Infrastructure/
│       ├── BackgroundJobs/
│       ├── Caching/
│       ├── Data/
│       ├── Identity/
│       ├── Repositories/
│       ├── Services/
│       └── Storage/
├── tests/
│   └── NexInvoice.UnitTests/
├── nexinvoice-web/
├── docker-compose.yml
└── NexInvoice.slnx
```

## Naming Conventions

- Project names use `NexInvoice.<Layer>`.
- Namespaces follow folder structure where practical, for example `NexInvoice.Application.Features.Invoices`.
- API routes use versioned prefixes: `/api/v1/...`.
- Application feature folders use business language:
  - `Customers` for client-facing customer workflows.
  - `WorkItems` for task/work tracking workflows.
  - `Invoices`, `Payments`, `Projects`, `Dashboard`, and `Auth` for their respective modules.
- Domain entity names remain singular, for example `Client`, `Project`, `Invoice`, `Payment`.
- DTOs use request/response suffixes:
  - `CreateInvoiceRequest`
  - `InvoiceResponse`
  - `ProjectListItemResponse`
- Service interfaces use the `I` prefix and stay in `Application/Interfaces`.
- Infrastructure implementations are grouped by responsibility: `Identity`, `Services`, `BackgroundJobs`, `Caching`, `Storage`, and `Data`.

## Git Commit Convention

Use a lightweight Conventional Commits style:

```text
feat: add invoice PDF generation
fix: prevent cancelled invoice from being marked paid
refactor: move application DTOs into feature folders
test: add payment business rule tests
docs: update Docker run instructions
chore: rename solution to NexInvoice
```

Recommended types:

- `feat`: New feature.
- `fix`: Bug fix.
- `refactor`: Internal code restructuring without behavior change.
- `test`: Test additions or updates.
- `docs`: Documentation changes.
- `chore`: Tooling, config, rename, or maintenance work.

## Database Design Summary

The database is organized around identity, customer management, project delivery, invoicing, payments, and notifications.

- Identity: `AppUser`, `Role`, `Permission`, `UserRole`, `RolePermission`, `RefreshToken`.
- Business: `Client`, `Project`, `TaskItem`, `TaskComment`, `TaskAttachment`, `Contract`.
- Finance: `Invoice`, `InvoiceItem`, `Payment`.
- Platform: `Notification`, `AuditLog`.

Persistence notes:

- Primary keys use `Guid`.
- Soft delete uses `IsDeleted`.
- EF Core relationships are configured with Fluent API.
- Money fields use decimal precision.
- Global query filters exclude soft-deleted rows.
- Startup initialization applies migrations, seeds system data, and can generate demo business data.

## API Documentation

Swagger UI:

```text
http://localhost:8080/swagger
```

Main API route convention:

```text
/api/v1/[controller]
```

Examples:

```text
POST   /api/v1/auth/register
POST   /api/v1/auth/login

GET    /api/v1/clients
POST   /api/v1/clients
GET    /api/v1/clients/{id}

GET    /api/v1/projects
PATCH  /api/v1/projects/{id}/status
GET    /api/v1/projects/{projectId}/tasks

GET    /api/v1/tasks/{id}
PATCH  /api/v1/tasks/{id}/status

GET    /api/v1/invoices
GET    /api/v1/invoices/{id}/pdf
PATCH  /api/v1/invoices/{id}/mark-paid

POST   /api/v1/payments
GET    /api/v1/invoices/{invoiceId}/payments
PATCH  /api/v1/payments/{id}/confirm

GET    /api/v1/notifications
GET    /api/v1/dashboard/summary
```

SignalR hub:

```text
/hubs/notifications
```

## Appsettings Structure

The API supports environment-specific configuration files:

```text
appsettings.json
appsettings.Development.json
appsettings.Staging.json
appsettings.Production.json
```

Keep secrets out of source control for staging and production. Prefer environment variables, Docker secrets, or deployment platform secret stores.

## How to Run Locally

Prerequisites:

- .NET 10 SDK
- SQL Server
- Redis
- Docker Desktop, optional

Backend:

```powershell
dotnet restore .\NexInvoice.slnx
dotnet build .\NexInvoice.slnx
dotnet run --project .\src\NexInvoice.API\NexInvoice.API.csproj
```

Tests:

```powershell
dotnet test .\NexInvoice.slnx
```

Frontend:

```powershell
cd nexinvoice-web
npm install
npm run dev
```

## Docker Setup

Start backend infrastructure:

```powershell
docker compose up --build
```

URLs:

```text
API:                http://localhost:8080
Swagger UI:         http://localhost:8080/swagger
Hangfire Dashboard: http://localhost:8080/hangfire
SignalR Hub:        http://localhost:8080/hubs/notifications
```

Stop:

```powershell
docker compose down
```

Stop and remove volumes:

```powershell
docker compose down -v
```

## Demo Account

```text
Email:    admin@nexinvoice.com
Password: Admin@123
Role:     Admin
```

## Vietnamese UI Note

The frontend UI and API response messages are intended for Vietnamese users. Code identifiers, namespaces, entities, DTOs, and routes remain in English for maintainability.

## Screenshots

Place screenshots under:

```text
docs/screenshots/login.png
docs/screenshots/dashboard.png
docs/screenshots/customers.png
docs/screenshots/project-detail.png
docs/screenshots/invoice-detail.png
docs/screenshots/notifications.png
```

## CV Bullet Points

- Built a Clean Architecture ASP.NET Core modular monolith for freelance invoice and project management.
- Organized the Application layer by features and separated Domain, Infrastructure, and API concerns.
- Implemented JWT authentication, refresh token rotation, BCrypt password hashing, and permission-based authorization.
- Designed EF Core persistence with SQL Server, Fluent API, soft delete filters, seed data, and migrations.
- Integrated SignalR, Hangfire, Redis caching, MailKit, QuestPDF, Docker, and xUnit tests.

## Future Improvements

- Add integration tests with Testcontainers.
- Add audit logging pipeline.
- Add production-ready file storage abstraction.
- Add CI/CD with build, test, Docker image publish, and deployment stages.
- Add frontend Docker support and reverse proxy configuration.
