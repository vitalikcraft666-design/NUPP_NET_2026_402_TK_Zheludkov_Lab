using GameStudio.Common.Models;

namespace GameStudio.REST.Models;

/// <summary>Дані художника у відповідях API (разом з обчисленими посадою, стажем і зарплатою).</summary>
public class ArtistModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateTime HireDate { get; set; }

    public decimal BaseSalary { get; set; }

    public ArtSpecialization Specialization { get; set; }

    public string MainTool { get; set; } = string.Empty;

    public int ArtworksCompleted { get; set; }

    public string Position { get; set; } = string.Empty;

    public int YearsOfExperience { get; set; }

    public decimal MonthlySalary { get; set; }
}
