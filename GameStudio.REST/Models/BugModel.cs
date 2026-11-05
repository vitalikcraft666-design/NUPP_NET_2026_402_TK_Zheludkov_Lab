using GameStudio.Common.Models;

namespace GameStudio.REST.Models;

/// <summary>Дані бага у відповідях API (GET /api/bugs, GET /api/bugs/{id}, POST, PUT).</summary>
public class BugModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public BugSeverity Severity { get; set; }

    public bool IsFixed { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid ProjectId { get; set; }

    public Guid? AssigneeId { get; set; }
}
