# CMS API - API Endpoints

## Base URL

```
/cms-api/v{version}/
```

Default version: `v1.0`

## Authentication

All endpoints (except login) require authentication via:
- **JWT Bearer Token** - Primary method
- **Basic Auth** - Legacy support

Include token in header:
```
Authorization: Bearer <jwt_token>
```

## Endpoints Reference

### Authentication

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/auth/login` | User login | Basic Auth |
| GET | `/v1.0/auth/GetPermissions` | Get user permissions | JWT |
| POST | `/v1.0/auth/logout` | User logout | JWT |

### Claims Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/claim/List` | Get paginated claims list | JWT |
| POST | `/v1.0/claim/Get` | Get claim details | JWT |
| POST | `/v1.0/claim/FailedLog` | Get failed claim logs | JWT |
| POST | `/v1.0/claim/ImagingLog` | Get imaging logs | JWT |
| POST | `/v1.0/claim/ImagingLogDetail` | Get imaging log details | JWT |
| POST | `/v1.0/claim/ClaimStatus` | Get claim status | JWT |

### Products

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/v1.0/products` | List all products | JWT |
| GET | `/v1.0/products/{id}` | Get product by ID | JWT |
| POST | `/v1.0/products` | Create new product | JWT |
| PUT | `/v1.0/products/{id}` | Update product | JWT |
| DELETE | `/v1.0/products/{id}` | Delete product | JWT |

### Coverages

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/v1.0/coverages` | List all coverages | JWT |
| GET | `/v1.0/coverages/{id}` | Get coverage by ID | JWT |
| POST | `/v1.0/coverages` | Create new coverage | JWT |
| PUT | `/v1.0/coverages/{id}` | Update coverage | JWT |

### Servicing

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/servicing/List` | Get service requests | JWT |
| POST | `/v1.0/servicing/Get` | Get service request details | JWT |
| POST | `/v1.0/servicing/Update` | Update service request | JWT |

### Members

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/members/List` | Get members list | JWT |
| POST | `/v1.0/members/Get` | Get member details | JWT |
| POST | `/v1.0/members/Update` | Update member | JWT |

### Member Policies

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/memberpolicy/List` | Get member policies | JWT |
| POST | `/v1.0/memberpolicy/Get` | Get policy details | JWT |

### Propositions

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/propositions/List` | Get propositions | JWT |
| POST | `/v1.0/propositions/Get` | Get proposition details | JWT |

### Notifications

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/notification/Send` | Send notification | JWT |
| POST | `/v1.0/notification/List` | Get notifications | JWT |

### Dashboard

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/v1.0/dashboard/Metrics` | Get dashboard metrics | JWT |
| GET | `/v1.0/dashboard/Analytics` | Get analytics data | JWT |

### Localization

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/localization/List` | Get localizations | JWT |
| POST | `/v1.0/localization/Update` | Update localization | JWT |

### Hospitals

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/hospital/List` | Get hospitals list | JWT |
| POST | `/v1.0/hospital/Get` | Get hospital details | JWT |
| POST | `/v1.0/hospital/Create` | Create hospital | JWT |
| POST | `/v1.0/hospital/Update` | Update hospital | JWT |

### Diagnosis

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/diagnosis/List` | Get diagnosis codes | JWT |
| POST | `/v1.0/diagnosis/Get` | Get diagnosis details | JWT |

### Insurance Types

#### Critical Illness
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/criticalIllness/List` | Get critical illness list | JWT |
| POST | `/v1.0/criticalIllness/Update` | Update critical illness | JWT |

#### Death Benefits
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/death/List` | Get death benefits | JWT |
| POST | `/v1.0/death/Update` | Update death benefit | JWT |

#### Disability
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/partialDisability/List` | Get partial disability | JWT |
| POST | `/v1.0/permanentDisability/List` | Get permanent disability | JWT |

### Content Management

#### Blogs
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/blogs/List` | Get blogs list | JWT |
| POST | `/v1.0/blogs/Create` | Create blog post | JWT |
| POST | `/v1.0/blogs/Update` | Update blog post | JWT |
| POST | `/v1.0/blogs/Delete` | Delete blog post | JWT |

#### FAQs
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/faq/List` | Get FAQs | JWT |
| POST | `/v1.0/faq/Create` | Create FAQ | JWT |
| POST | `/v1.0/faq/Update` | Update FAQ | JWT |

### Administration

#### Banks
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/bank/List` | Get banks list | JWT |
| POST | `/v1.0/bank/Update` | Update bank | JWT |

#### Staff Management
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/staffs/List` | Get staff list | JWT |
| POST | `/v1.0/staffs/Create` | Create staff | JWT |
| POST | `/v1.0/staffs/Update` | Update staff | JWT |
| POST | `/v1.0/staffs/Delete` | Delete staff | JWT |

#### Roles & Permissions
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/roles/List` | Get roles list | JWT |
| POST | `/v1.0/roles/Create` | Create role | JWT |
| POST | `/v1.0/roles/Update` | Update role | JWT |

#### Holidays
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/holiday/List` | Get holidays | JWT |
| POST | `/v1.0/holiday/Update` | Update holiday | JWT |

### Configuration

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/document-config/List` | Get document configs | JWT |
| POST | `/v1.0/document-config/Update` | Update document config | JWT |
| POST | `/v1.0/paymentchange-config/List` | Get payment configs | JWT |
| POST | `/v1.0/paymentchange-config/Update` | Update payment config | JWT |

### Files

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/file/Upload` | Upload file | JWT |
| GET | `/v1.0/file/Download/{id}` | Download file | JWT |

### CRM Integration

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/crm/Sync` | Sync with CRM | JWT |
| POST | `/v1.0/crm/Push` | Push data to CRM | JWT |

### User Profile

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/v1.0/profile` | Get current user profile | JWT |
| PUT | `/v1.0/profile` | Update profile | JWT |

### Master Data

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/master/List` | Get master data | JWT |

### Claim Incurred Location

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/claimincurredlocation/List` | Get claim locations | JWT |
| POST | `/v1.0/claimincurredlocation/Update` | Update claim location | JWT |

### Development & Migration

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/v1.0/dev/*` | Development endpoints | JWT |
| POST | `/v1.0/migrate/*` | Data migration utilities | JWT |

## Request/Response Examples

### Login Request

```http
POST /cms-api/v1.0/auth/login
Authorization: Basic base64(username:password)
Content-Type: application/json
{
  "email": "tinlinnnsoe@codigo.co",
  "password": "Codigo200$$"
}

```

Response:
```json
{
  "code": 0,
  "message": "string",
  "data": {
    "accessToken": "string",
    "permission": {
      "staffId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "staffEmail": "string",
      "roleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "roleName": "string",
      "permissions": [
        "string"
      ]
    }
  }
}
```
### AD Login Response Callback

```http
POST /cms-api/v1.0/saml/acs
```

Response:
```json
"SamlUrl": {
    "Success": "https://uat.aia.com.mm/aiaplus/admin/login",
    "Fail": "https://uat.aia.com.mm/aiaplus/admin/login/error"
  },
```

```csharp
 ResponseModel<string> response = authRepository.ADLogin(email);
 devRepository.ErrorLog($"OktaCallBack authRepository response: {JsonConvert.SerializeObject(response)}");
 if (response.Code==0)
 {    
     redirectUrl = config["SamlUrl:Success"] + "?token=" + response.Data;
 }
 else
 {
     redirectUrl = config["SamlUrl:Fail"] + "?message=" + response.Message;
 }
 devRepository.ErrorLog(redirectUrl);
 return Redirect(redirectUrl);

https://uat.aia.com.mm/aiaplus/admin/login?token=JwtToken
https://uat.aia.com.mm/aiaplus/admin/login/error?message=Error message
```
### Get Claims List

```http
POST /cms-api/v1/claim
Authorization: Bearer <jwt_token>
Content-Type: application/json

{
  "page": 1,
  "size": 10,
  "memberName": "string",
  "clientNo": "string",
  "requestId": "string",
  "detailId": "string",
  "policyNo": "string",
  "memberPhone": "string",
  "fromDate": "2026-01-08T07:30:31.216Z",
  "toDate": "2026-01-08T07:30:31.216Z",
  "claimStatus": "string",
  "ilStatus": "Success",
  "claimType": "string",
  "claimTypeList": [
    "string"
  ]
}
```

Response:
```json
{
  "code": 0,
  "message": "string",
  "data": {
    "totalCount": 0,
    "totalPage": 0,
    "currentPage": 0,
    "pageSize": 0,
    "hasNextPage": true,
    "hasPreviousPage": true,
    "dataList": [
      {
        "mainClaimId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "claimId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "memberName": "string",
        "clientNo": "string",
        "groupClientNo": "string",
        "memberType": "string",
        "memberPhone": "string",
        "policyNo": "string",
        "claimType": "string",
        "claimStatus": "string",
        "claimStatusCode": "string",
        "remainingHour": "string",
        "ilStatus": "string",
        "updatedBy": "string",
        "updatedDt": "2026-01-08T07:30:31.255Z",
        "tranDate": "2026-01-08T07:30:31.255Z",
        "productType": "string",
        "createdDt": "2026-01-08T07:30:31.255Z",
        "appMemberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "diagnosisId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "diagnosisName": "string",
        "claimFormType": "string",
        "diagnosisNameEn": "string",
        "causedByNameEn": "string"
      }
    ]
  }
}
```

### Error Response

```json
{
    "code": 400,
    "message": "Validation error",
    "data": null,
    "errors": [
        "Field 'claimId' is required"
    ]
}
```

## Swagger Documentation

Swagger UI is available at:
```
/cms-api/swagger
```

Protected with basic authentication.
