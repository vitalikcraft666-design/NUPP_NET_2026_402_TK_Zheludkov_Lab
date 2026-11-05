namespace GameStudio.REST.Models;

/// <summary>ЛР5: дані користувача (GET /api/account/me, список користувачів для адміністратора).</summary>
public class AccountModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public DateTime RegisteredAt { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
