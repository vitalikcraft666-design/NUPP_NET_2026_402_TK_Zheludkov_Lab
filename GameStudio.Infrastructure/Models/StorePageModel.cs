namespace GameStudio.Infrastructure.Models;

/// <summary>Модель БД для доменного класу StorePage (таблиця StorePages, один-до-одного з GameProjects).</summary>
public class StorePageModel
{
    public int Id { get; set; }

    // зовнішній ключ і водночас унікальний індекс — гарантує, що в проєкту не більше однієї сторінки
    public int GameProjectId { get; set; }

    public GameProjectModel GameProject { get; set; } = null!;

    public string Url { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int WishlistCount { get; set; }

    public DateTime? ReleaseDate { get; set; }
}
