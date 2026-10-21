using System.Text.Json.Serialization;
using GameStudio.Common.Interfaces;

namespace GameStudio.Common.Models;

// делегат — описує сигнатуру методів-обробників зміни статусу проєкту
public delegate void ProjectStatusChangedHandler(GameProject project, ProjectStatus oldStatus, ProjectStatus newStatus);

/// <summary>
/// Ігровий проєкт, над яким працює студія.
/// </summary>
public class GameProject : IEntity
{
    // статичне поле — множина рушіїв (engine), які підтримує студія
    private static readonly HashSet<string> SupportedEngines;

    // статичний конструктор — один раз заповнює статичне поле
    static GameProject()
    {
        SupportedEngines = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Unity",
            "Unreal Engine",
            "Godot",
            "Roblox Studio"
        };
    }

    // конструктор без параметрів (потрібен для десеріалізації з JSON)
    public GameProject()
    {
        Id = Guid.NewGuid();
        Title = string.Empty;
        Genre = string.Empty;
        Engine = string.Empty;
        Status = ProjectStatus.Concept;
    }

    // конструктор з параметрами — спочатку викликає конструктор без параметрів через this()
    public GameProject(string title, string genre, string engine, decimal budget)
        : this()
    {
        // статичний метод викликається всередині конструктора для перевірки даних
        if (!IsEngineSupported(engine))
        {
            throw new ArgumentException($"Рушій \"{engine}\" не підтримується студією.", nameof(engine));
        }

        if (budget <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget), "Бюджет має бути додатним.");
        }

        Title = title;
        Genre = genre;
        Engine = engine;
        Budget = budget;
    }

    // подія на основі власного делегата ProjectStatusChangedHandler
    public event ProjectStatusChangedHandler? StatusChanged;

    // подія на основі вбудованого делегата Action<T1, T2>: (проєкт, сума перевитрати)
    public event Action<GameProject, decimal>? BudgetExceeded;

    public Guid Id { get; set; }

    public string Title { get; set; }

    public string Genre { get; set; }

    public string Engine { get; set; }

    public decimal Budget { get; set; }

    // приватний сеттер + [JsonInclude]: змінювати можна лише через метод Spend, але JSON усе одно зберігає значення
    [JsonInclude]
    public decimal Spent { get; private set; }

    [JsonInclude]
    public ProjectStatus Status { get; private set; }

    public List<Guid> TeamMemberIds { get; set; } = new();

    // статичний метод — перевіряє, чи підтримується рушій
    public static bool IsEngineSupported(string engine) => SupportedEngines.Contains(engine);

    // статичний метод — повертає копію списку підтримуваних рушіїв
    public static IReadOnlyCollection<string> GetSupportedEngines() => SupportedEngines.ToList();

    // метод — змінює статус проєкту та викликає подію StatusChanged
    public void ChangeStatus(ProjectStatus newStatus)
    {
        if (Status is ProjectStatus.Released or ProjectStatus.Cancelled)
        {
            throw new InvalidOperationException($"Проєкт \"{Title}\" уже завершено ({Status}), статус змінити не можна.");
        }

        if (newStatus == Status)
        {
            return;
        }

        ProjectStatus oldStatus = Status;
        Status = newStatus;

        // виклик події (якщо є підписники)
        StatusChanged?.Invoke(this, oldStatus, newStatus);
    }

    // метод — фіксує витрати; якщо бюджет перевищено, викликає подію BudgetExceeded
    public void Spend(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Сума витрат має бути додатною.");
        }

        bool wasWithinBudget = Spent <= Budget;
        Spent += amount;

        if (wasWithinBudget && Spent > Budget)
        {
            BudgetExceeded?.Invoke(this, Spent - Budget);
        }
    }

    // метод — додає працівника до команди проєкту
    public void AddTeamMember(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        if (!TeamMemberIds.Contains(employee.Id))
        {
            TeamMemberIds.Add(employee.Id);
        }
    }

    // перевизначений метод ToString() класу object — текстове представлення об'єкта
    public override string ToString()
    {
        return $"\"{Title}\" ({Genre}, {Engine}) — {Status}, бюджет: {Budget:N0} грн, витрачено: {Spent:N0} грн, у команді: {TeamMemberIds.Count}";
    }
}
