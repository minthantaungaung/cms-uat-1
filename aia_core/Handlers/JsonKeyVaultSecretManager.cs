using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace aia_core.Handlers
{
    public class JsonKeyVaultSecretManager : KeyVaultSecretManager
    {
        private readonly string _secretName;

        public JsonKeyVaultSecretManager(string secretName)
        {
            _secretName = secretName;
        }

        public override bool Load(SecretProperties secret)
        {
            // Only load the secret that exactly matches the name
            return secret.Name.Equals(_secretName, StringComparison.OrdinalIgnoreCase);
        }

        public override string GetKey(KeyVaultSecret secret)
        {
            // This is required by Azure provider; we return the secret name itself
            return secret.Name;
        }
    }
}
