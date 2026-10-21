using GameStudio.Common.Models;

namespace GameStudio.Infrastructure.Models;

/// <summary>Модель БД для доменного класу GameProject (таблиця GameProjects).</summary>
public class GameProjectModel : IDbModel
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public string Engine { get; set; } = string.Empty;

    public decimal Budget { get; set; }

    public decimal Spent { get; set; }

    public ProjectStatus Status { get; set; }

    // один-до-одного: сторінка в магазині
    public StorePageModel? StorePage { get; set; }

    // один-до-багатьох: баги проєкту
    public List<BugReportModel> Bugs { get; set; } = new();

    // багато-до-багатьох: учасники команди
    public List<EmployeeModel> TeamMembers { get; set; } = new();

    // проміжна сутність зв'язку багато-до-багатьох
    public List<ProjectAssignmentModel> Assignments { get; set; } = new();
}
