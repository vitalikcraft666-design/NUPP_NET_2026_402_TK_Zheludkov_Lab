namespace GameStudio.Common.Interfaces;

/// <summary>
/// ЛР3: шаблон проєктування «Репозиторій» — абстракція доступу до даних.
/// Інтерфейс описано в Common, а реалізації — в інфраструктурних проєктах:
/// GameStudio.Infrastructure (EF Core + SQLite) і GameStudio.Nosql (MongoDB).
/// </summary>
public interface IRepository<T>
    where T : class
{
    Task<T> GetByIdAsync(int id);

    Task<IEnumerable<T>> GetAllAsync();

    Task AddAsync(T entity);

    Task Update(T entity);

    Task Delete(T entity);
}
