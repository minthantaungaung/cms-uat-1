# CMS API - Architecture

## Architecture Overview

The CMS API follows a clean layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      Presentation Layer                      │
│                     (cms_api/Controllers)                    │
│              37 REST API Controllers + Filters               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Application Layer                       │
│                    (aia_core/Services)                       │
│         Business Logic + External API Integrations           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Data Access Layer                         │
│                   (aia_core/Repository)                      │
│          32 CMS Repositories + Unit of Work Pattern          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Database Layer                          │
│                   (aia_core/Entities)                        │
│      139 Entity Models + Entity Framework Core DbContext     │
└─────────────────────────────────────────────────────────────┘
```

## Design Patterns

### 1. Repository Pattern

Each domain has a dedicated repository class that encapsulates data access logic:

```csharp
// Example: ClaimRepository
public class ClaimRepository : Repository<Claim>, IClaimRepository
{
    public async Task<PagedList<Claim>> GetClaimsAsync(ClaimFilterRequest filter)
    {
        // Data access logic
    }
}
```

Key Repositories:
- `ClaimRepository` - Claims management
- `ServicingRepository` - Service requests
- `ProductRepository` - Product catalog
- `MemberRepository` - Member data
- `NotificationRepository` - Notifications

### 2. Unit of Work Pattern

Transaction management across multiple repositories:

```csharp
public interface IUnitOfWork<TContext> where TContext : DbContext
{
    Task<int> SaveChangesAsync();
    void BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();
}
```
### 3. Dependency Injection

All services and repositories are registered in `Program.cs`:

```csharp

builder.Services.AddTransient<IClaimRepository, ClaimRepository>();
builder.Services.AddTransient<INotificationService, NotificationService>();

builder.Services.AddScoped<IRecurringJobRunner, RecurringJobRunner>();
builder.Services.AddSingleton<ITemplateLoader, TemplateLoader>();
builder.Services.AddSingleton<ICmsTokenGenerator, CmsTokenGenerator>();
```

## Authentication Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Client    │────▶│  API Gate   │────▶│ Auth Handler│
└─────────────┘     └─────────────┘     └─────────────┘
                                               │
                    ┌──────────────────────────┼──────────────────────────┐
                    ▼                          ▼                          ▼
            ┌─────────────┐            ┌─────────────┐            ┌─────────────┐
            │  JWT Bearer │            │ Basic Auth  │            │ Custom Auth │
            │  (Auth0)    │            │ (Legacy)    │            │ (CRM)       │
            └─────────────┘            └─────────────┘            └─────────────┘
```

### Authentication Handlers

| Handler | Purpose | Location |
|---------|---------|----------|
| `BasicAuthHandler` | Standard basic authentication | `cms_api/Handlers/` |
| `CustomBasicAuthHandler` | CRM integration auth | `cms_api/Handlers/` |
| JWT Bearer | Token-based authentication | Built-in middleware |

### Permission Validation

Custom middleware validates user permissions per route:

```
Request → PermissionValidationMiddleware → Controller Action
```

## Background Job Architecture

Hangfire manages scheduled background jobs:

```
┌─────────────────────────────────────────────────────────────┐
│                    Hangfire Server                           │
│                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Claim Noti Job  │  │ Service Noti    │  │ SMS Jobs    │ │
│  │ Daily 1:30 AM   │  │ Daily 1:30 AM   │  │ 11:30 AM    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Member Data Pull│  │ Premium Noti    │  │ Claim Status│ │
│  │ Daily 9:30 PM   │  │ Daily 1:30 AM   │  │ Update      │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Scheduled Jobs

| Job Name | Schedule | Purpose |
|----------|----------|---------|
| `SendClaimNotification` | Daily 1:30 AM UTC | Claim status notifications |
| `SendServiceNotification` | Daily 1:30 AM UTC | Service request notifications |
| `CheckBeneficiaryStatusAndSendNoti` | Daily 1:30 AM UTC | Beneficiary update alerts |
| `UpdateClaimStatus` | Daily 1:30 AM UTC | Sync claim statuses |
| `SendUpcomingPremiumsNotification` | Daily 1:30 AM UTC | Premium due reminders |
| `UpdateMemberDataPullFromAiaCoreTables` | Daily 9:30 PM UTC | Member data sync |
| `SendClaimSms` | Daily 11:30 AM UTC | SMS notifications |
| `SendServicingSms` | Daily 11:30 AM UTC | Servicing SMS |
| `UploadDefaultCmsImages` | On startup | Initialize default images |

## Data Flow

### Request Processing Flow

```
1. HTTP Request
       │
       ▼
2. Authentication Middleware (JWT/Basic Auth)
       │
       ▼
3. Permission Validation Middleware
       │
       ▼
4. Action Filters (NoSingleQuoteActionFilter)
       │
       ▼
5. Controller Action
       │
       ▼
6. Service Layer (Business Logic)
       │
       ▼
7. Repository Layer (Data Access)
       │
       ▼
8. Entity Framework Core (ORM)
       │
       ▼
9. SQL Server Database
```

### Response Format

All API responses follow a standard wrapper:

```json
{
    "code": 0,
    "message": "Success",
    "data": { ... }
}
```

### Pagination

Paginated responses use `PagedList<T>`:

```json
{
    "code": 0,
    "message": "Success",
    "data": {
         "totalCount": 100,
         "totalPage": 10,
         "currentPage": 1,
         "pageSize": 10,
         "hasNextPage": true,
         "hasPreviousPage": false,
         "data": [ ... ]
    }
}
```

## External Service Integration

```
┌─────────────────┐
│    CMS API      │
└────────┬────────┘
         │
    ┌────┴────┬──────────┬──────────┬──────────┐
    ▼         ▼          ▼          ▼          ▼
┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐
│D365CRM│ │ Azure │ │Firebase│ │ Okta  │ │ClamAV │
│  API  │ │ Blob  │ │  FCM   │ │  SSO  │ │ Scan  │
└───────┘ └───────┘ └───────┘ └───────┘ └───────┘
```

## Security Architecture

### Input Validation

- `NoSingleQuoteActionFilter` - Prevents SQL injection via single quotes
- Request model validation with data annotations
- File upload validation with size limits (10 MB max)
- ClamAV malware scanning for uploaded files

### Authentication Layers

1. **Transport Security** - HTTPS with SSL certificates
2. **Token Validation** - JWT Bearer with Auth0
3. **Permission Checks** - Custom permission middleware
4. **Request Sanitization** - Input filters and validation
