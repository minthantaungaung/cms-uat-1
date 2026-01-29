# CMS API - Configuration

## Configuration Files

Configuration files are located in `cms_api/bk-appsettings/`:

| File | Environment |
|------|-------------|
| `appsettings.Development.json` | Local development |
| `appsettings.Uat.json` | UAT environment |
| `appsettings.Dr.json` | Disaster recovery |
| `appsettings.Production.json` | Production |


## Configuration Sections

### Environment Settings

```json
{
  "Env": "Uat"
}
```

Valid values: `Development`, `Uat`, `Dr`, `Production`

### Database Configuration

```json
{
  "Database": {
    "ConnectionString": "Server=<server>;Database=<database>;User Id=<user>;Password=<password>;TrustServerCertificate=True;"
  }
}
```

### JWT Configuration

```json
{
  "JWT": {
    "SecretKey": "<32-character-secret-key>",
    "Issuer": "aia-cms-api",
    "Audience": "aia-cms-client",
    "ExpirationMinutes": 60
  }
}
```

### Auth0 Configuration

```json
{
  "Auth0": {
    "Domain": "dev-xxxxx.us.auth0.com",
    "Audience": "https://api.aia.com.mm",
    "ClientId": "<client-id>",
    "ClientSecret": "<client-secret>"
  }
}
```

### Azure Blob Storage

```json
{
  "Azure": {
    "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "ContainerName": "cms-files",
    "BlobBaseUrl": "https://<account>.blob.core.windows.net"
  }
}
```

### Azure Key Vault

```json
{
  "KeyVault": {
    "Endpoint": "https://kv-xxxxx.vault.azure.net/",
    "ManagedIdentityId": "<managed-identity-guid>",
    "SecretName": "uat-cms-appsettings"
  }
}
```

### Okta SSO Configuration

```json
{
  "Okta": {
    "Domain": "https://aia-uat.okta.com",
    "ClientId": "<client-id>",
    "ClientSecret": "<client-secret>",
    "AuthorizationServerId": "default",
    "RedirectUri": "https://cms.aia.com.mm/callback"
  }
}
```

### Microsoft D365 CRM Integration

```json
{
  "AiaCrm": {
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "ClientSecret": "<client-secret>",
    "Resource": "https://org.crm.dynamics.com",
    "ApiBaseUrl": "https://apigw01-uat.aia.com.mm/MMCRMD365/",
    "OAuthEndpoint": "https://login.microsoftonline.com/<tenant>/oauth2/token"
  }
}
```

### SMS Provider (POH)

```json
{
  "SmsPoh": {
    "ApiUrl": "https://sms-provider.com/api/send",
    "ApiKey": "<api-key>",
    "SenderId": "AIA"
  }
}
```

### ClamAV Antivirus

```json
{
  "ClamAVServer": {
    "Host": "clamav",
    "Port": 3310,
    "Timeout": 30000
  }
}
```

### Firebase Cloud Messaging

```json
{
  "Firebase": {
    "ProjectId": "<project-id>",
    "CredentialPath": "/app/firebase-credentials.json"
  }
}
```

### Hangfire Configuration

```json
{
  "Hangfire": {
    "DashboardUsername": "admin",
    "DashboardPassword": "<password>",
    "WorkerCount": 5
  }
}
```

### SAML Configuration

```json
{
  "SamlUrl": {
    "RedirectUrl": "https://cms.aia.com.mm/saml/redirect",
    "MetadataUrl": "https://idp.aia.com.mm/metadata"
  }
}
```

### Basic Authentication

```json
{
  "BasicAuth": {
    "Username": "codigo",
    "Password": "<password>"
  },
  "CrmBasicAuth": {
    "Username": "aiaplussystemcrm",
    "Password": "<password>"
  }
}
```

### File Upload Settings

```json
{
  "FileUpload": {
    "MaxFileSizeBytes": 10485760,
    "AllowedExtensions": [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"]
  }
}
```

### Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## Environment Variables

The application also supports environment variables for sensitive configurations:

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment |
| `DATABASE_CONNECTION_STRING` | Database connection |
| `JWT_SECRET_KEY` | JWT signing key |
| `AZURE_STORAGE_CONNECTION` | Azure storage |
| `AUTH0_CLIENT_SECRET` | Auth0 secret |

## Configuration Loading Order

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific)
3. Environment variables
4. Azure Key Vault secrets (if configured)

## Secrets Management

Production secrets are managed through:

1. **Azure Key Vault** - Primary secrets storage
2. **Environment Variables** - Container runtime
3. **Kubernetes Secrets** - Kubernetes deployment

Never commit sensitive values to source control. Use placeholder values in committed configuration files.

## SSL Certificates

SSL certificates are stored in `cms_api/ssl/`:

| File | Environment |
|------|-------------|
| `uat.pfx` | UAT certificate |
| `production.pfx` | Production certificate |

Configure in `Program.cs` for HTTPS:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps("ssl/certificate.pfx", "password");
    });
});
```

## Migration
Environment configurations are migrated to **Azure Key Vault** due to AIA Group Security Policy.
Connecting to Azure Key Vault in **Program.Cs** and reading all config values at once and stored at application startup 

**Migrated for UAT and Production already**.

**Environment identifying**
```csharp
if(environment.ToLower() == "uat")
{
   manageIdentityClientId = "9941156c-93d3-4957-b2e1-f8c5ee2a9c98";
   azureVaultUri = "https://kv-mm01-sea-u-app-vlt01.vault.azure.net/";
   secretName = "uat-cms-appsettings";
}
else if (environment.ToLower() == "production")
{
   manageIdentityClientId = "f6fb2ff0-665b-443c-a94b-93d66a601031";
   azureVaultUri = "https://kv-mm01-sea-p-app-vlt01.vault.azure.net/";
   secretName = "prod-cms-appsettings";
}
```

**Connectiong and reading**
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri(azureVaultUri),
    new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ManagedIdentityClientId = manageIdentityClientId
    }),
    new JsonKeyVaultSecretManager(secretName) // single JSON secret
);
```

**Parsing String as Json and Translate as Dictionary object to be readable by application**
```csharp
// After loading, parse the JSON secret manually
var secretString = builder.Configuration[secretName];

if (!string.IsNullOrEmpty(secretString))
{
    var jsonData = JsonDocument.Parse(secretString);
    var dict = new Dictionary<string, string>();

    void Flatten(JsonElement element, string prefix = "")
    {
        foreach (var prop in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}:{prop.Name}";
            if (prop.Value.ValueKind == JsonValueKind.Object)
                Flatten(prop.Value, key);
            else
                dict[key] = prop.Value.ToString();
        }
    }

    Flatten(jsonData.RootElement);
    builder.Configuration.AddInMemoryCollection(dict);
}
```
