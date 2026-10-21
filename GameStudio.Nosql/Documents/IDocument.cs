namespace GameStudio.Nosql.Documents;

/// <summary>Документ MongoDB з цілочисельним ідентифікатором (поле _id), з яким працює IRepository&lt;T&gt;.</summary>
public interface IDocument
{
    int Id { get; set; }
}
