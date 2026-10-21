using GameStudio.Common.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameStudio.Nosql.Documents;

/// <summary>
/// Проєкт у колекції projects. Замість зв'язків між таблицями пов'язані дані вкладені в документ:
/// сторінка в магазині (1:1), баги (1:N) і копії даних учасників команди (N:M, денормалізація).
/// Один запит повертає проєкт разом з усім, що до нього належить, — без JOIN.
/// </summary>
public class GameProjectDocument : IDocument
{
    [BsonId]
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public string Engine { get; set; } = string.Empty;

    public decimal Budget { get; set; }

    public decimal Spent { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ProjectStatus Status { get; set; }

    [BsonIgnoreIfNull]
    public StorePageDocument? StorePage { get; set; }

    public List<TeamMemberDocument> Team { get; set; } = new();

    public List<BugDocument> Bugs { get; set; } = new();
}

/// <summary>Вкладений документ: сторінка гри в магазині.</summary>
public class StorePageDocument
{
    public string Url { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int WishlistCount { get; set; }

    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(DateOnly = true)]
    public DateTime? ReleaseDate { get; set; }
}

/// <summary>Вкладений документ: учасник команди (посилання на employees._id + копія імені та ролі).</summary>
public class TeamMemberDocument
{
    public int EmployeeId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

/// <summary>Вкладений документ: баг проєкту.</summary>
public class BugDocument
{
    public int BugId { get; set; }

    public string Title { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public BugSeverity Severity { get; set; }

    public bool IsFixed { get; set; }

    [BsonDateTimeOptions(DateOnly = true)]
    public DateTime CreatedAt { get; set; }

    [BsonIgnoreIfNull]
    public int? AssigneeId { get; set; }

    [BsonIgnoreIfNull]
    public string? AssigneeName { get; set; }
}
