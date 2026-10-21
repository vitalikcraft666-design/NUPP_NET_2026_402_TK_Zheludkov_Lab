using GameStudio.Common.Models;

namespace GameStudio.Infrastructure.Models;

/// <summary>Модель БД для доменного класу Programmer (таблиця Programmers, TPT).</summary>
public class ProgrammerModel : EmployeeModel
{
    public string PrimaryLanguage { get; set; } = string.Empty;

    public ProgrammerGrade Grade { get; set; }

    public int CommitsCount { get; set; }

    // один-до-багатьох: баги, призначені програмісту
    public List<BugReportModel> AssignedBugs { get; set; } = new();
}
