# CMS API - Database Entities

## Overview

The CMS API uses Entity Framework Core with 139 entity models stored in `aia_core/Entities/`. The database is SQL Server hosted on Azure.

## Entity Categories

### Core Domain Entities

#### Members
| Entity | Description |
|--------|-------------|
| `Member` | Core member information |
| `MemberClient` | Client association |
| `MemberSession` | User sessions |
| `MemberDevice` | Registered devices |

#### Policies
| Entity | Description |
|--------|-------------|
| `Policy` | Insurance policy details |
| `PolicyStatus` | Policy status tracking |
| `PremiumStatus` | Premium payment status |
| `PolicyAdditionalAmt` | Additional amounts |

#### Claims
| Entity | Description |
|--------|-------------|
| `Claim` | Claim records |
| `ClaimStatus` | Claim status history |
| `ClaimBenefit` | Claimed benefits |
| `ClaimDocument` | Attached documents |
| `ClaimIncurredLocation` | Location of claim incident |

#### Products
| Entity | Description |
|--------|-------------|
| `Product` | Insurance products |
| `ProductType` | Product categories |
| `ProductBenefit` | Product benefits |
| `ProductCoverage` | Coverage details |

#### Insurance Types
| Entity | Description |
|--------|-------------|
| `CriticalIllness` | Critical illness coverage |
| `Death` | Death benefits |
| `PartialDisability` | Partial disability |
| `PermanentDisability` | Permanent disability |
| `Diagnosis` | Diagnosis codes |

#### Servicing
| Entity | Description |
|--------|-------------|
| `ServiceMainDoc` | Main service documents |
| `ServiceBeneficiaryPersonalInfo` | Beneficiary details |
| `ServicePartialWithdraw` | Partial withdrawal requests |
| `ServicePolicySurrender` | Policy surrender requests |

#### Coverage
| Entity | Description |
|--------|-------------|
| `Coverage` | Coverage definitions |
| `Hospital` | Hospital network |
| `CoverageRepository` | Coverage data access |

#### Business
| Entity | Description |
|--------|-------------|
| `Blog` | Blog posts |
| `Beneficiary` | Beneficiary information |
| `Bank` | Bank master data |
| `Staff` | CMS staff users |
| `Role` | User roles |
| `Client` | Client records |

#### Configuration
| Entity | Description |
|--------|-------------|
| `AppConfig` | Application settings |
| `DocConfig` | Document configuration |
| `PaymentChangeConfig` | Payment change settings |
| `CashlessClaimConfig` | Cashless claim settings |

#### Support
| Entity | Description |
|--------|-------------|
| `FAQ` | Frequently asked questions |
| `Localization` | Multi-language content |
| `Holiday` | Holiday calendar |

#### Notifications
| Entity | Description |
|--------|-------------|
| `Notification` | Notification records |
| `CmsNotification` | CMS-specific notifications |
| `PushNotificationLog` | Push notification history |

#### Logging
| Entity | Description |
|--------|-------------|
| `ErrorLogCms` | CMS error logs |
| `ErrorLogMobile` | Mobile error logs |
| `AuditLog` | Audit trail |

## DbContext Configuration

The `Context` class in `aia_core/Entities/Context.cs` configures all entity mappings:

```csharp
public class Context : DbContext
{
    public DbSet<Member> Members { get; set; }
    public DbSet<Policy> Policies { get; set; }
    public DbSet<Claim> Claims { get; set; }
    public DbSet<Product> Products { get; set; }
    // ... 100+ DbSet mappings
}
```

## Key Entity Relationships

### Member - Policy Relationship
```
Member (1) ──────< (N) Policy
   │
   └──────< (N) MemberDevice
   │
   └──────< (N) MemberSession
```

### Policy - Claim Relationship
```
Policy (1) ──────< (N) Claim
                       │
                       └──────< (N) ClaimDocument
                       │
                       └──────< (N) ClaimBenefit
                       │
                       └──────< (N) ClaimStatus
```

### Product - Coverage Relationship
```
Product (1) ──────< (N) ProductCoverage
    │
    └──────< (N) ProductBenefit
```

### Staff - Role Relationship
```
Role (1) ──────< (N) Staff
  │
  └──────< (N) Permission
```

## Common Entity Properties

Most entities include these common fields:

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid/int | Primary key |
| `CreatedAt` | DateTime | Creation timestamp |
| `CreatedBy` | string | Creator user ID |
| `UpdatedAt` | DateTime? | Last update timestamp |
| `UpdatedBy` | string? | Last updater user ID |
| `IsActive` | bool | Soft delete flag |

## Entity Statistics

| Category | Count |
|----------|-------|
| Total Entities | 139 |
| DbSet Mappings | 100+ |
| Core Domain Entities | ~50 |
| Configuration Entities | ~15 |
| Logging Entities | ~10 |
| Support Entities | ~10 |

## Database Connection

Connection strings are configured per environment in `appsettings.{Environment}.json`:

```json
{
  "Database": {
    "ConnectionString": "Server=...;Database=...;..."
  }
}
```

## Migrations

Entity Framework migrations are managed through:

```bash
# Add migration
dotnet ef migrations add MigrationName --project aia_core

# Update database
dotnet ef database update --project aia_core
```
