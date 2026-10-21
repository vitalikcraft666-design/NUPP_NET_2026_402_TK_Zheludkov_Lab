using GameStudio.Common.Models;

namespace GameStudio.Infrastructure.Models;

/// <summary>Модель БД для доменного класу Artist (таблиця Artists, TPT).</summary>
public class ArtistModel : EmployeeModel
{
    public ArtSpecialization Specialization { get; set; }

    public string MainTool { get; set; } = string.Empty;

    public int ArtworksCompleted { get; set; }
}
