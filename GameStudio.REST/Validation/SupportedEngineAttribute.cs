using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Models;

namespace GameStudio.REST.Validation;

/// <summary>
/// Атрибут перевірки: рушій гри має входити до списку, який підтримує студія (GameProject.GetSupportedEngines()).
/// Завдяки [ApiController] помилка перевірки автоматично повертається клієнту як 400 Bad Request.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class SupportedEngineAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string engine && GameProject.IsEngineSupported(engine))
        {
            return ValidationResult.Success;
        }

        string supported = string.Join(", ", GameProject.GetSupportedEngines());
        return new ValidationResult($"Рушій \"{value}\" не підтримується. Допустимі значення: {supported}.",
            new[] { validationContext.MemberName ?? "Engine" });
    }
}
