using System.ComponentModel.DataAnnotations;

namespace GameStudio.REST.Models;

/// <summary>
/// Сторінка гри в магазині — використовується і у відповідях, і як тіло PUT /api/projects/{id}/store-page.
/// Ідентифікатора немає: сторінка не існує окремо від проєкту (зв'язок один-до-одного).
/// </summary>
public class StorePageModel
{
    [Required(ErrorMessage = "Адреса сторінки обов'язкова.")]
    [Url(ErrorMessage = "Некоректна адреса сторінки.")]
    [StringLength(200)]
    public string Url { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "100000", ErrorMessage = "Ціна має бути від 0 до 100 000 грн.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Кількість у списку бажаного не може бути від'ємною.")]
    public int WishlistCount { get; set; }

    public DateTime? ReleaseDate { get; set; }
}
