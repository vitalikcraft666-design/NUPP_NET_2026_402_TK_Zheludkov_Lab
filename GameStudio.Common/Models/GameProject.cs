using System.Text.Json.Serialization;
using GameStudio.Common.Generation;
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

    // ЛР2: статичний фабричний метод — створює проєкт з випадково згенерованими даними
    public static GameProject CreateNew()
    {
        var project = new GameProject(
            RandomData.GameTitle(),
            RandomData.Pick(RandomData.Genres),
            RandomData.Pick(SupportedEngines.ToArray()),
            RandomData.Money(50_000m, 2_000_000m, 1_000m));

        // частина бюджету вже витрачена: від 0 до 120 %
        decimal spent = Math.Round(project.Budget * (decimal)(Random.Shared.NextDouble() * 1.2), 2);
        if (spent > 0)
        {
            project.Spend(spent);
        }

        // випадковий етап розробки
        ProjectStatus status = RandomData.PickEnum<ProjectStatus>();
        if (status != ProjectStatus.Concept)
        {
            project.ChangeStatus(status);
        }

        // команда з 2–15 учасників
        int teamSize = Random.Shared.Next(2, 16);
        for (int i = 0; i < teamSize; i++)
        {
            project.TeamMemberIds.Add(Guid.NewGuid());
        }

        // ЛР3: сторінка в магазині з'являється на етапі виробництва
        if (status is ProjectStatus.Production or ProjectStatus.Testing or ProjectStatus.Released)
        {
            project.StorePage = StorePage.CreateNew(project.Title, released: status == ProjectStatus.Released);
        }

        return project;
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

    // ЛР3: сторінка гри в магазині (зв'язок «один-до-одного»; null — сторінки ще немає)
    public StorePage? StorePage { get; set; }

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
