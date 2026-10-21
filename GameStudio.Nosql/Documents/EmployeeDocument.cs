using GameStudio.Common.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameStudio.Nosql.Documents;

/// <summary>
/// Працівник у колекції employees. Наслідування в MongoDB: усі типи зберігаються в одній колекції,
/// а конкретний клас визначається полем-дискримінатором _t (на відміну від окремих таблиць у TPT).
/// </summary>
[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(ProgrammerDocument), typeof(ArtistDocument))]
public abstract class EmployeeDocument : IDocument
{
    [BsonId]
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public string FullName { get; set; } = string.Empty;

    [BsonDateTimeOptions(DateOnly = true)]
    public DateTime HireDate { get; set; }

    public decimal BaseSalary { get; set; }
}

[BsonDiscriminator("Programmer")]
public class ProgrammerDocument : EmployeeDocument
{
    public string PrimaryLanguage { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public ProgrammerGrade Grade { get; set; }

    public int CommitsCount { get; set; }
}

[BsonDiscriminator("Artist")]
public class ArtistDocument : EmployeeDocument
{
    [BsonRepresentation(BsonType.String)]
    public ArtSpecialization Specialization { get; set; }

    public string MainTool { get; set; } = string.Empty;

    public int ArtworksCompleted { get; set; }
}
