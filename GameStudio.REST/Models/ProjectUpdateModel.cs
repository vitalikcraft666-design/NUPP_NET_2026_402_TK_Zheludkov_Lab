using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Models;
using GameStudio.REST.Validation;

namespace GameStudio.REST.Models;

/// <summary>
/// Тіло запиту PUT /api/projects/{id}: на відміну від ProjectCreateModel містить статус.
/// Витрати (Spent) через API не змінюються, тому цієї властивості тут немає.
/// </summary>
public class ProjectUpdateModel
{
    [Required(ErrorMessage = "Назва обов'язкова.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Назва має містити від 2 до 100 символів.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Жанр обов'язковий.")]
    [StringLength(50, ErrorMessage = "Жанр — не більше 50 символів.")]
    public string Genre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Рушій обов'язковий.")]
    [SupportedEngine]
    public string Engine { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "1000000000", ErrorMessage = "Бюджет має бути від 1 до 1 000 000 000 грн.")]
    public decimal Budget { get; set; }

    [EnumDataType(typeof(ProjectStatus), ErrorMessage = "Невідомий статус проєкту.")]
    public ProjectStatus Status { get; set; }
}
