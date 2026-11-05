using Microsoft.AspNetCore.Identity;

namespace GameStudio.Infrastructure.Identity;

/// <summary>
/// ЛР5: користувач застосунку. Успадковує від IdentityUser стандартні властивості ASP.NET Core Identity
/// (Id, UserName, Email, PasswordHash, SecurityStamp, LockoutEnd тощо) і додає власні.
/// Таблиця — AspNetUsers.
/// </summary>
public class StudioUser : IdentityUser
{
    /// <summary>ПІБ користувача (змінюється через PUT /api/account/me).</summary>
    public string? FullName { get; set; }

    /// <summary>Дата й час реєстрації (UTC), заповнює StudioUserManager.</summary>
    public DateTime RegisteredAt { get; set; }
}
