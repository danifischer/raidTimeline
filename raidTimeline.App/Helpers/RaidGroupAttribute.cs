using System.ComponentModel.DataAnnotations;

namespace raidTimeline.App.Helpers;

public class RaidGroupAttribute : ValidationAttribute
{
    private readonly string[] _validValues = {
        "Static1", "Static2", "Training", "Random", "Scourge_Static"
    };
    
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string raidGroup)
        {
            return _validValues.Contains(raidGroup) 
                ? ValidationResult.Success!
                : new ValidationResult($"The value '{value}' is not a valid input.");
        }

        return new ValidationResult($"The value '{value}' is not a string.");
    }
}