using GameStudio.Common.Models;

namespace GameStudio.REST.Models;

/// <summary>
/// Дані програміста у відповідях API. Окрім збережених полів містить обчислені
/// (посада, стаж, місячна зарплата), які рахує доменна модель Programmer.
/// </summary>
public class ProgrammerModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateTime HireDate { get; set; }

    public decimal BaseSalary { get; set; }

    public string PrimaryLanguage { get; set; } = string.Empty;

    public ProgrammerGrade Grade { get; set; }

    public int CommitsCount { get; set; }

    public string Position { get; set; } = string.Empty;

    public int YearsOfExperience { get; set; }

    public decimal MonthlySalary { get; set; }
}
