# CMS API Technical Documentation

This folder contains the technical documentation for the CMS API project.

## Documentation Index

| Document | Description |
|----------|-------------|
| [01-overview.md](./01-overview.md) | Project overview, technology stack, and features |
| [02-architecture.md](./02-architecture.md) | System architecture, design patterns, and data flow |
| [03-api-endpoints.md](./03-api-endpoints.md) | Complete API endpoint reference |
| [04-database-entities.md](./04-database-entities.md) | Database entities and relationships |
| [05-configuration.md](./05-configuration.md) | Configuration settings and environment setup |
| [06-deployment.md](./06-deployment.md) | Docker, Kubernetes, and CI/CD deployment |

## Quick Start

1. Clone the repository
2. Environment configuration in **Program.Cs**  and all configuration values are setting up in **Azure Key Vault**.
   ```bash
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
3. Update configuration values
4. Run the application:
   ```bash
   cd cms_api
   dotnet run
   ```

## API Access

- **Base URL**: `/cms-api/v1.0/`
- **Swagger UI**: `/cms-api/docs/api/index.html`
  - Credential
     - Username: codigoaia
     - Password: aia123!@# 

- **Hangfire Dashboard**: `/cms-api/hangfire`
   - Credential
        - Username: admin
        - Password: codigo180@a!a99


## Support

For questions or issues, contact the development team.
