using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Security
{
    public static class AES
    {
        public static string Decrypt(string value)

        {

            byte[] passwordBytes = Encoding.UTF8.GetBytes(AppSettingsHelper.GetSetting("AES:encKey"));
            byte[] vectorBytes = Encoding.UTF8.GetBytes(AppSettingsHelper.GetSetting("AES:vectorBytes"));
            byte[] saltBytes = Encoding.UTF8.GetBytes(AppSettingsHelper.GetSetting("AES:saltBytes"));
            byte[] valueBytes = Convert.FromBase64String(value);

            string decrypted = "";

            using (var cipher = Aes.Create())
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                cipher.BlockSize = 128;
                cipher.KeySize = 256;
                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.PKCS7;
                cipher.Key = key.GetBytes(16);
                cipher.IV = vectorBytes;

                using (ICryptoTransform decryptor = cipher.CreateDecryptor())
                {
                    using (MemoryStream from = new MemoryStream(valueBytes))
                    {
                        using (CryptoStream reader = new CryptoStream(from, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(reader, Encoding.UTF8))
                            {
                                decrypted = sr.ReadToEnd();
                            }
                        }
                    }
                }
                cipher.Clear();
            }

            return decrypted;
        }
    }
}
