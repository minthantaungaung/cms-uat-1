using System;
using System.Security.Cryptography;

namespace aia_core.Provider
{
    public class PasswordManager
    {
        // Number of iterations for password hashing (adjust as needed)
        private const int Iterations = 10;
        // Size of the salt in bytes
        private const int SaltSize = 16;
        // Size of the resulting hash in bytes
        private const int HashSize = 32;

        public static (string hash, string salt) CreatePasswordHashAndSalt(string password)
        {
            // Generate a random salt
            byte[] saltBytes = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // Create the password hash
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations))
            {
                byte[] hashBytes = pbkdf2.GetBytes(HashSize);
                string hash = Convert.ToBase64String(hashBytes);
                return (hash, salt);
            }
        }

        public static bool ValidatePassword(string password, string storedHash, string storedSalt)
        {
            // Convert the stored salt and hash from base64
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] storedHashBytes = Convert.FromBase64String(storedHash);

            // Compute the hash of the entered password with the stored salt
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations))
            {
                byte[] hashBytes = pbkdf2.GetBytes(HashSize);

                // Compare the computed hash with the stored hash
                for (int i = 0; i < HashSize; i++)
                {
                    if (hashBytes[i] != storedHashBytes[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}