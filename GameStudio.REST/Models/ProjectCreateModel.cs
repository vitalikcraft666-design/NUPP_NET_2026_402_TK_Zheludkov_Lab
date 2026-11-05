using System.ComponentModel.DataAnnotations;
using GameStudio.REST.Validation;

namespace GameStudio.REST.Models;

/// <summary>
/// Тіло запиту POST /api/projects. Немає Id (його генерує сервер), статусу (новий проєкт завжди Concept)
/// і витрат (на старті 0) — лише ті властивості, які клієнт справді задає.
/// </summary>
public class ProjectCreateModel
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
}
