using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.Validators;

public static class DateValidator
{
    public static ValidationResult? Validate(DateTime? birthDate)
    {
        if (birthDate.HasValue && birthDate.Value.Date > DateTime.Today)
            return new ValidationResult("Date cannot be in the future.");
        return ValidationResult.Success;
    }
}