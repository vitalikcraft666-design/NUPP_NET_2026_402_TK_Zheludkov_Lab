using System.Text.Json.Serialization;

namespace GameStudio.Common.Models;

/// <summary>
/// Художник — наслідується від абстрактного класу Employee.
/// </summary>
public class Artist : Employee
{
    // константа (неявно статична) — бонус за кожну завершену роботу
    public const decimal BonusPerArtwork = 250m;

    // конструктор без параметрів
    public Artist()
    {
        MainTool = string.Empty;
    }

    // конструктор з параметрами — викликає конструктор базового класу через base(...)
    public Artist(string fullName, decimal baseSalary, ArtSpecialization specialization, string mainTool, DateTime? hireDate = null)
        : base(fullName, baseSalary, hireDate)
    {
        Specialization = specialization;
        MainTool = mainTool;
    }

    public ArtSpecialization Specialization { get; set; }

    public string MainTool { get; set; }

    public int ArtworksCompleted { get; set; }

    [JsonIgnore]
    public override string Position => Specialization switch
    {
        ArtSpecialization.Concept => "Концепт-художник",
        ArtSpecialization.Modeling3D => "3D-моделер",
        ArtSpecialization.Animation => "Аніматор",
        ArtSpecialization.UserInterface => "UI-художник",
        _ => "Художник"
    };

    // метод — художник завершив ще одну роботу (модель, анімацію, екран UI тощо)
    public void CompleteArtwork()
    {
        ArtworksCompleted++;
    }

    // перевизначення (override) абстрактного методу базового класу
    public override decimal CalculateMonthlySalary()
    {
        return Math.Round(ApplyExperienceBonus(BaseSalary) + ArtworksCompleted * BonusPerArtwork, 2);
    }
}
