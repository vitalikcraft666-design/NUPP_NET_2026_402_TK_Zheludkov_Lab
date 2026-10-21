using System.Text.Json.Serialization;
using GameStudio.Common.Generation;

namespace GameStudio.Common.Models;

/// <summary>
/// Програміст — наслідується від абстрактного класу Employee.
/// </summary>
public class Programmer : Employee
{
    // конструктор без параметрів (викликає конструктор базового класу)
    public Programmer()
    {
        PrimaryLanguage = string.Empty;
    }

    // конструктор з параметрами — передає спільні дані в конструктор базового класу через base(...)
    public Programmer(string fullName, decimal baseSalary, string primaryLanguage, ProgrammerGrade grade, DateTime? hireDate = null)
        : base(fullName, baseSalary, hireDate)
    {
        PrimaryLanguage = primaryLanguage;
        Grade = grade;
    }

    // ЛР2: статичний фабричний метод — створює програміста з випадково згенерованими даними
    public static Programmer CreateNew()
    {
        var programmer = new Programmer(
            RandomData.FullName(),
            RandomData.Money(20_000m, 60_000m, 500m),
            RandomData.Pick(RandomData.ProgrammingLanguages),
            RandomData.PickEnum<ProgrammerGrade>(),
            RandomData.Date(new DateTime(2015, 1, 1), DateTime.Today));

        programmer.CommitsCount = Random.Shared.Next(0, 5001);
        return programmer;
    }

    public string PrimaryLanguage { get; set; }

    public ProgrammerGrade Grade { get; set; }

    public int CommitsCount { get; set; }

    [JsonIgnore]
    public override string Position => $"{Grade} {PrimaryLanguage}-програміст";

    // метод — фіксує нові коміти програміста
    public void MakeCommits(int count = 1)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Кількість комітів має бути додатною.");
        }

        CommitsCount += count;
    }

    // метод — підвищення грейду; повертає false, якщо грейд уже максимальний
    public bool Promote()
    {
        if (Grade == ProgrammerGrade.Lead)
        {
            return false;
        }

        Grade++;
        return true;
    }

    // перевизначення (override) абстрактного методу базового класу
    public override decimal CalculateMonthlySalary()
    {
        decimal gradeMultiplier = Grade switch
        {
            ProgrammerGrade.Junior => 1.0m,
            ProgrammerGrade.Middle => 1.5m,
            ProgrammerGrade.Senior => 2.0m,
            ProgrammerGrade.Lead => 2.5m,
            _ => 1.0m
        };

        return Math.Round(ApplyExperienceBonus(BaseSalary * gradeMultiplier), 2);
    }
}
