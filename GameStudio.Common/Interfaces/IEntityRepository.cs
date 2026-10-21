namespace GameStudio.Common.Interfaces;

/// <summary>
/// ЛР3: розширення базового <see cref="IRepository{T}"/> для потреб CRUD-сервісу:
/// пошук за публічним ідентифікатором (Guid доменної моделі), пагінація на рівні БД
/// і фіксація накопичених змін (одиниця роботи, Unit of Work).
/// </summary>
public interface IEntityRepository<T> : IRepository<T>
    where T : class
{
    Task<T?> GetByPublicIdAsync(Guid publicId);

    Task<IEnumerable<T>> GetPageAsync(int page, int amount);

    Task<int> CountAsync();

    Task<int> SaveChangesAsync();
}
