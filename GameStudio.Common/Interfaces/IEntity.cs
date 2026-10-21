namespace GameStudio.Common.Interfaces;

/// <summary>
/// Спільний контракт для всіх сутностей, які можна зберігати у CRUD-сервісі.
/// Кожна сутність має унікальний ідентифікатор.
/// </summary>
public interface IEntity
{
    Guid Id { get; set; }
}
