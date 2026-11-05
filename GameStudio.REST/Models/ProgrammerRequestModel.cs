using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Models;

namespace GameStudio.REST.Models;

/// <summary>
/// Тіло запитів POST /api/programmers і PUT /api/programmers/{id}: лише ті поля, які задає клієнт
/// (без Id та обчислюваних властивостей Position / YearsOfExperience / MonthlySalary).
/// </summary>
public class ProgrammerRequestModel
{
    [Required(ErrorMessage = "ПІБ обов'язкове.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "ПІБ має містити від 3 до 100 символів.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Дата прийняття на роботу; якщо не вказано — сьогодні.</summary>
    public DateTime? HireDate { get; set; }

    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "Ставка має бути від 1 до 1 000 000 грн.")]
    public decimal BaseSalary { get; set; }

    [Required(ErrorMessage = "Мова програмування обов'язкова.")]
    [StringLength(30)]
    public string PrimaryLanguage { get; set; } = string.Empty;

    [EnumDataType(typeof(ProgrammerGrade), ErrorMessage = "Невідомий грейд.")]
    public ProgrammerGrade Grade { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Кількість комітів не може бути від'ємною.")]
    public int CommitsCount { get; set; }
}
