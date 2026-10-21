namespace GameStudio.Infrastructure.Models;

/// <summary>
/// Модель БД для доменного класу Employee. Абстрактний базовий клас ієрархії,
/// відображається за стратегією «таблиця на тип» (TPT): спільні поля — у таблиці Employees,
/// специфічні — у таблицях Programmers та Artists.
/// </summary>
public abstract class EmployeeModel : IDbModel
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateTime HireDate { get; set; }

    public decimal BaseSalary { get; set; }

    // багато-до-багатьох: проєкти, у командах яких працює співробітник
    public List<GameProjectModel> Projects { get; set; } = new();

    // ті самі зв'язки через проміжну сутність (з роллю та датою призначення)
    public List<ProjectAssignmentModel> Assignments { get; set; } = new();
}
