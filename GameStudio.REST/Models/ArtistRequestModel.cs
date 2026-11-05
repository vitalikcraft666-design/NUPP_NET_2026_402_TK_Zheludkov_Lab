using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Models;

namespace GameStudio.REST.Models;

/// <summary>Тіло запитів POST /api/artists і PUT /api/artists/{id} (без Id та обчислюваних властивостей).</summary>
public class ArtistRequestModel
{
    [Required(ErrorMessage = "ПІБ обов'язкове.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "ПІБ має містити від 3 до 100 символів.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Дата прийняття на роботу; якщо не вказано — сьогодні.</summary>
    public DateTime? HireDate { get; set; }

    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "Ставка має бути від 1 до 1 000 000 грн.")]
    public decimal BaseSalary { get; set; }

    [EnumDataType(typeof(ArtSpecialization), ErrorMessage = "Невідома спеціалізація.")]
    public ArtSpecialization Specialization { get; set; }

    [Required(ErrorMessage = "Основний інструмент обов'язковий.")]
    [StringLength(50)]
    public string MainTool { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Кількість робіт не може бути від'ємною.")]
    public int ArtworksCompleted { get; set; }
}
