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
