using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Models;

namespace GameStudio.REST.Models;

/// <summary>
/// Тіло запиту PUT /api/bugs/{id}. Проєкт бага не змінюється, тому ProjectId тут немає,
/// натомість можна змінити виконавця і позначити баг виправленим.
/// </summary>
public class BugUpdateModel
{
    [Required(ErrorMessage = "Опис бага обов'язковий.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Опис має містити від 3 до 200 символів.")]
    public string Title { get; set; } = string.Empty;

    [EnumDataType(typeof(BugSeverity), ErrorMessage = "Невідома критичність.")]
    public BugSeverity Severity { get; set; }

    public Guid? AssigneeId { get; set; }

    public bool IsFixed { get; set; }
}
