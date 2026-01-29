# CMS API - Project Overview

## Introduction

The CMS API is an enterprise-level Content Management System backend for AIA Myanmar's insurance platform. It provides RESTful APIs for managing insurance products, claims, policies, members, and various administrative functions.

## Technology Stack

| Component | Technology | Version |
|-----------|------------|---------|
| Framework | ASP.NET Core | 8.0 |
| Language | C# | Latest |
| Shared Library | .NET | 7.0 |
| Database | SQL Server | Azure SQL |
| ORM | Entity Framework Core | 7.0.9 |
| Background Jobs | Hangfire | 1.8.5 |
| Cloud Storage | Azure Blob Storage | - |
| API Documentation | Swagger/OpenAPI | Swashbuckle |
| Container | Docker | Alpine-based |

## Project Structure

```
cms_api/
├── Controllers/          # 37 API controllers for REST endpoints
├── Filters/              # Action filters (e.g., NoSingleQuoteActionFilter)
├── Handlers/             # Authentication handlers (BasicAuth, CustomBasicAuth)
├── Properties/           # Assembly metadata
├── bk-appsettings/       # Environment-specific configuration files
├── ssl/                  # SSL certificate files (UAT & Production)
├── docs/                 # Technical documentation (this folder)
├── Program.cs            # Application startup & dependency injection
├── cms_api.csproj        # Project file
└── Dockerfile            # Docker containerization

aia_core/ (Shared Library)
├── Entities/             # 139 database entity models
├── Repository/           # Data access layer
│   └── Cms/              # 32 CMS-specific repositories
├── Services/             # Business logic services
│   └── AIA/              # AIA-specific service integrations
├── Model/                # DTOs for requests/responses
│   └── Cms/
│       ├── Request/      # 42 request model classes
│       └── Response/     # 42 response model classes
├── UnitOfWork/           # Unit of Work pattern implementation
├── Handlers/             # Security & authentication handlers
├── RecurringJobs/        # Hangfire background job runner
└── Extension/            # Custom extension methods
```

## Core Features

1. **Insurance Product Management** - CRUD operations for insurance products, coverages, and benefits
2. **Claims Processing** - Claim submission, tracking, status updates, and document management
3. **Member Management** - Member registration, profile management, and policy associations
4. **Servicing Requests** - Service request processing (beneficiary changes, withdrawals, surrenders)
5. **Notifications** - Push notifications, email, and SMS integration
6. **Dashboard & Analytics** - Metrics and reporting for administrative users
7. **Content Management** - Blog posts, FAQs, and localization
8. **User & Role Management** - Staff accounts, roles, and permissions

## Authentication Methods

| Method | Use Case |
|--------|----------|
| JWT Bearer Token | Primary API authentication |
| Basic Authentication | Legacy endpoints |
| Custom Basic Auth | CRM system integration |
| Okta SSO | Single Sign-On for admin users |

## External Integrations

- **Microsoft D365 CRM** - Customer relationship management
- **Azure Key Vault** - Secrets management
- **Firebase Admin SDK** - Push notifications
- **ClamAV** - Malware scanning
- **Auth0** - JWT token provider
- **Okta** - Identity management

## Environments

| Environment | Purpose |
|-------------|---------|
| Development | Local development |
| UAT | User Acceptance Testing |
| DR | Disaster Recovery |
| Production | Live production environment |

## API Base URL

```
/cms-api/v{version}/
```

Default API version: `v1.0`
