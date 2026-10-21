namespace GameStudio.Infrastructure.Models;

/// <summary>
/// Проміжна (join) сутність зв'язку «багато-до-багатьох» між проєктами та працівниками
/// (таблиця ProjectAssignments) з додатковими полями: роль у команді та дата призначення.
/// </summary>
public class ProjectAssignmentModel
{
    public int GameProjectId { get; set; }

    public GameProjectModel GameProject { get; set; } = null!;

    public int EmployeeId { get; set; }

    public EmployeeModel Employee { get; set; } = null!;

    public string Role { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; }
}
