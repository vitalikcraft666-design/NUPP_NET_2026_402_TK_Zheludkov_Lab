using System.Text.Json.Serialization;
using GameStudio.Common.Interfaces;

namespace GameStudio.Common.Models;

/// <summary>
/// Абстрактний базовий клас для всіх працівників ігрової студії.
/// Атрибути JsonPolymorphic / JsonDerivedType потрібні, щоб під час серіалізації
/// зберігався конкретний тип (Programmer чи Artist).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Programmer), "programmer")]
[JsonDerivedType(typeof(Artist), "artist")]
public abstract class Employee : IEntity
{
    // статичне поле — лічильник усіх створених об'єктів-працівників (спільний для всіх екземплярів)
    private static int _createdCount;

    // статичне поле — надбавка до зарплати за кожен повний рік стажу (3 % = 0.03)
    public static readonly decimal ExperienceBonusPerYear;

    // статичний конструктор — викликається один раз, перед першим зверненням до класу
    static Employee()
    {
        _createdCount = 0;
        ExperienceBonusPerYear = 0.03m;
    }

    // конструктор без параметрів (потрібен для десеріалізації з JSON)
    protected Employee()
        : this(string.Empty, 0m)
    {
    }

    // конструктор з параметрами
    protected Employee(string fullName, decimal baseSalary, DateTime? hireDate = null)
    {
        Id = Guid.NewGuid();
        FullName = fullName;
        BaseSalary = baseSalary;
        HireDate = hireDate ?? DateTime.Today;
        _createdCount++;
    }

    public Guid Id { get; set; }

    public string FullName { get; set; }

    public DateTime HireDate { get; set; }

    public decimal BaseSalary { get; set; }

    /// <summary>Назва посади — кожен нащадок визначає її сам.</summary>
    [JsonIgnore]
    public abstract string Position { get; }

    // статичний метод — повертає значення статичного поля, викликається через ім'я класу: Employee.GetCreatedCount()
    public static int GetCreatedCount() => _createdCount;

    // метод — кількість повних років стажу на вказану дату (за замовчуванням — на сьогодні)
    public int GetYearsOfExperience(DateTime? onDate = null)
    {
        DateTime date = onDate ?? DateTime.Today;
        int years = date.Year - HireDate.Year;
        if (HireDate.Date > date.AddYears(-years))
        {
            years--;
        }

        return Math.Max(years, 0);
    }

    // абстрактний метод — кожен тип працівника рахує зарплату по-своєму
    public abstract decimal CalculateMonthlySalary();

    // метод — застосовує надбавку за стаж до суми (доступний лише класам-нащадкам)
    protected decimal ApplyExperienceBonus(decimal amount)
    {
        return amount * (1 + ExperienceBonusPerYear * GetYearsOfExperience());
    }

    // перевизначений метод (override) класу object
    public override string ToString()
    {
        return $"{Position,-28} {FullName,-22} стаж: {GetYearsOfExperience()} р., зарплата: {CalculateMonthlySalary():N2} грн";
    }
}
