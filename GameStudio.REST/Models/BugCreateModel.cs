using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Models;

namespace GameStudio.REST.Models;

/// <summary>
/// Тіло запиту POST /api/bugs. Немає Id, дати створення (ставить сервер) і ознаки IsFixed
/// (новий баг завжди відкритий).
/// </summary>
public class BugCreateModel
{
    [Required(ErrorMessage = "Опис бага обов'язковий.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Опис має містити від 3 до 200 символів.")]
    public string Title { get; set; } = string.Empty;

    [EnumDataType(typeof(BugSeverity), ErrorMessage = "Невідома критичність.")]
    public BugSeverity Severity { get; set; }

    /// <summary>Проєкт, у якому знайдено баг (має існувати).</summary>
    [Required(ErrorMessage = "Потрібно вказати проєкт.")]
    public Guid? ProjectId { get; set; }

    /// <summary>Програміст-виконавець (необов'язково).</summary>
    public Guid? AssigneeId { get; set; }
}
