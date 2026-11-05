using GameStudio.Common.Models;

namespace GameStudio.REST.Models;

/// <summary>
/// Короткі дані проєкту для списку (GET /api/projects): без сторінки в магазині та команди,
/// щоб відповідь зі списком не містила зайвих властивостей.
/// </summary>
public class ProjectSummaryModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public string Engine { get; set; } = string.Empty;

    public ProjectStatus Status { get; set; }

    public decimal Budget { get; set; }

    public decimal Spent { get; set; }
}
