using System.ComponentModel.DataAnnotations;

namespace raidTimeline.App.Helpers;

internal class DayAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string day)
        {
            try
            {
                var date = new DateTime(
                    Convert.ToInt32(day.Substring(0, 4)),
                    Convert.ToInt32(day.Substring(4, 2)),
                    Convert.ToInt32(day.Substring(6, 2)));
                
                return day == date.ToString("yyyyMMdd")
                    ? ValidationResult.Success!
                    : new ValidationResult($"The value '{value}' is not a valid date.");
            }
            catch (Exception)
            {
                return new ValidationResult($"The value '{value}' is not a valid date.");
            }
        }
        
        return new ValidationResult($"The value '{value}' is not a string.");
    }
}