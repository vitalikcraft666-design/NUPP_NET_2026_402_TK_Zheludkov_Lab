using System.ComponentModel.DataAnnotations;

namespace GameStudio.REST.Models;

/// <summary>ЛР5: тіло PUT /api/account/me — користувач може змінити лише своє ПІБ (не email і не ролі).</summary>
public class AccountUpdateModel
{
    [Required(ErrorMessage = "ПІБ обов'язкове.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "ПІБ має містити від 3 до 100 символів.")]
    public string FullName { get; set; } = string.Empty;
}
