namespace GameStudio.Infrastructure.Models;

/// <summary>
/// Спільний контракт моделей БД, з якими працює CRUD-сервіс:
/// Id — первинний ключ таблиці (ціле число, генерується БД),
/// PublicId — альтернативний ключ, який дорівнює Guid-ідентифікатору доменного об'єкта.
/// </summary>
public interface IDbModel
{
    int Id { get; set; }

    Guid PublicId { get; set; }
}
