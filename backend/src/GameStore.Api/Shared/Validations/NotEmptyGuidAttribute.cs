using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.Shared.Validations;

public class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute()
        : base("{0} cannot be empty."){}

    public override bool IsValid(object? value)
        => value is Guid guidValue && guidValue != Guid.Empty;
}
