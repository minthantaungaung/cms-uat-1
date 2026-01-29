using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Extension
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Reflection;

    namespace aia_core.Extension
    {
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        public class NotEqualToPropertyAttribute : ValidationAttribute
        {
            private readonly string _otherPropertyName;

            public NotEqualToPropertyAttribute(string otherPropertyName)
            {
                _otherPropertyName = otherPropertyName;
            }

            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            {
                var currentValue = value as string;

                if (string.IsNullOrWhiteSpace(currentValue))
                    return ValidationResult.Success; // Let other validations handle empty passwords

                // Get the property info of the other property (e.g., "Username")
                PropertyInfo? otherProperty = validationContext.ObjectType.GetProperty(_otherPropertyName);

                if (otherProperty == null)
                    return new ValidationResult($"Unknown property: {_otherPropertyName}");

                var otherValue = otherProperty.GetValue(validationContext.ObjectInstance) as string;

                // Compare ignoring case and whitespace
                if (!string.IsNullOrEmpty(otherValue) && string.Equals(currentValue.Trim(), otherValue.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult(ErrorMessage ?? $"Password must not be the same as {_otherPropertyName}.");
                }

                return ValidationResult.Success;
            }
        }
    }

}
