using GameStudio.Common.Generation;
using GameStudio.Common.Interfaces;

namespace GameStudio.Common.Models;

/// <summary>
/// Звіт про помилку (баг), знайдену в ігровому проєкті.
/// </summary>
public class BugReport : IEntity
{
    // конструктор без параметрів (потрібен для десеріалізації з JSON)
    public BugReport()
    {
        Id = Guid.NewGuid();
        Title = string.Empty;
        CreatedAt = DateTime.Now;
    }

    // конструктор з параметрами
    public BugReport(string title, BugSeverity severity, Guid projectId)
        : this()
    {
        Title = title;
        Severity = severity;
        ProjectId = projectId;
    }

    // ЛР2: статичний фабричний метод — створює баг з випадково згенерованими даними
    public static BugReport CreateNew() => CreateNew(Guid.NewGuid());

    // ЛР2: перевантаження — баг для конкретного проєкту
    public static BugReport CreateNew(Guid projectId)
    {
        var bug = new BugReport(RandomData.BugTitle(), RandomData.PickEnum<BugSeverity>(), projectId)
        {
            CreatedAt = RandomData.Date(DateTime.Today.AddDays(-90), DateTime.Today)
        };

        // приблизно половину багів уже призначено виконавцю, частину з них — виправлено
        if (Random.Shared.Next(2) == 0)
        {
            bug.AssigneeId = Guid.NewGuid();
            bug.IsFixed = Random.Shared.Next(2) == 0;
        }

        return bug;
    }

    // подія на основі вбудованого делегата EventHandler (object? sender, EventArgs e)
    public event EventHandler? Fixed;

    public Guid Id { get; set; }

    public string Title { get; set; }

    public BugSeverity Severity { get; set; }

    public Guid ProjectId { get; set; }

    public Guid? AssigneeId { get; set; }

    public bool IsFixed { get; set; }

    public DateTime CreatedAt { get; set; }

    // метод — призначає баг програмісту
    public void AssignTo(Programmer programmer)
    {
        ArgumentNullException.ThrowIfNull(programmer);
        AssigneeId = programmer.Id;
    }

    // метод — позначає баг виправленим і викликає подію Fixed
    public void MarkAsFixed()
    {
        if (AssigneeId is null)
        {
            throw new InvalidOperationException($"Баг \"{Title}\" ще нікому не призначено.");
        }

        if (IsFixed)
        {
            return;
        }

        IsFixed = true;
        Fixed?.Invoke(this, EventArgs.Empty);
    }

    // перевизначений метод ToString() класу object — текстове представлення об'єкта
    public override string ToString()
    {
        string state = IsFixed ? "виправлено" : "відкрито";
        return $"[{Severity,-8}] {Title} — {state}";
    }
}
