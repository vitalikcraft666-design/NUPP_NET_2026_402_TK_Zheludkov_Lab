using GameStudio.Common.Models;

namespace GameStudio.Infrastructure.Models;

/// <summary>Модель БД для доменного класу BugReport (таблиця BugReports).</summary>
public class BugReportModel : IDbModel
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public string Title { get; set; } = string.Empty;

    public BugSeverity Severity { get; set; }

    public bool IsFixed { get; set; }

    public DateTime CreatedAt { get; set; }

    // багато-до-одного: проєкт, у якому знайдено баг (обов'язковий зв'язок)
    public int GameProjectId { get; set; }

    public GameProjectModel GameProject { get; set; } = null!;

    // багато-до-одного: програміст-виконавець (необов'язковий зв'язок)
    public int? AssigneeId { get; set; }

    public ProgrammerModel? Assignee { get; set; }
}
