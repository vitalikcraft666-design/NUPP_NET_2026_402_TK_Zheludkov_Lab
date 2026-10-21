using System.Text.Json.Serialization;
using GameStudio.Common.Generation;

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

    // ЛР2: статичний фабричний метод — створює художника з випадково згенерованими даними
    public static Artist CreateNew()
    {
        var artist = new Artist(
            RandomData.FullName(),
            RandomData.Money(18_000m, 50_000m, 500m),
            RandomData.PickEnum<ArtSpecialization>(),
            RandomData.Pick(RandomData.ArtTools),
            RandomData.Date(new DateTime(2015, 1, 1), DateTime.Today));

        artist.ArtworksCompleted = Random.Shared.Next(0, 41);
        return artist;
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
