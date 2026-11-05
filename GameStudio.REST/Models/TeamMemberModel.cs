namespace GameStudio.REST.Models;

/// <summary>Учасник команди проєкту (GET /api/projects/{id}/team) — лише дані, потрібні для списку команди.</summary>
public class TeamMemberModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>"Programmer" або "Artist" — за цим значенням можна перейти до /api/programmers/{id} чи /api/artists/{id}.</summary>
    public string Type { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;
}
