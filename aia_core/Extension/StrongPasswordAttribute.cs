using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Extension
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Text.RegularExpressions;

    namespace aia_core.Extension
    {
        public class StrongPasswordAttribute : ValidationAttribute
        {
            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            {
                var password = value as string;

                if (string.IsNullOrWhiteSpace(password))
                    return new ValidationResult("Password cannot be empty.");

                if (!IsStrongPassword(password))
                    return new ValidationResult("Password must be at least 8 characters long, contain upper and lower case letters, a digit, a special character, and must not have simple repeating patterns.");

                return ValidationResult.Success;
            }

            private bool IsStrongPassword(string password)
            {
                if (string.IsNullOrWhiteSpace(password)) return false;

                var hasMinimum8Chars = password.Length >= 8;
                var hasUpperCase = Regex.IsMatch(password, "[A-Z]");
                var hasLowerCase = Regex.IsMatch(password, "[a-z]");
                var hasDigit = Regex.IsMatch(password, "[0-9]");
                var hasSpecialChar = Regex.IsMatch(password, "[^a-zA-Z0-9]");

                var allSameChar = Regex.IsMatch(password, @"^(.)\1+$");
                var repeatedCouple = Regex.IsMatch(password, @"^((\w)\2)+$");

                bool hasRepeatingDoublePairs = false;
                if (password.Length % 2 == 0)
                {
                    hasRepeatingDoublePairs = true;
                    for (int i = 0; i < password.Length; i += 2)
                    {
                        if (password[i] != password[i + 1])
                        {
                            hasRepeatingDoublePairs = false;
                            break;
                        }
                    }
                }

                return hasMinimum8Chars && hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar
                    && !allSameChar && !repeatedCouple && !hasRepeatingDoublePairs;
            }
        }
    }

}
