using GameStudio.Common.Models;

namespace GameStudio.REST.Models;

/// <summary>Повні дані одного проєкту (GET /api/projects/{id}, відповіді на POST і PUT).</summary>
public class ProjectModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public string Engine { get; set; } = string.Empty;

    public ProjectStatus Status { get; set; }

    public decimal Budget { get; set; }

    public decimal Spent { get; set; }

    /// <summary>Залишок бюджету (від'ємний — бюджет перевищено).</summary>
    public decimal Remaining { get; set; }

    /// <summary>Кількість учасників команди (сам склад — GET /api/projects/{id}/team).</summary>
    public int TeamSize { get; set; }

    /// <summary>Сторінка в магазині (зв'язок один-до-одного); null — сторінки ще немає.</summary>
    public StorePageModel? StorePage { get; set; }
}
