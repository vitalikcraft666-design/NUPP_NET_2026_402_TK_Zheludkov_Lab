namespace GameStudio.Common.Interfaces;

/// <summary>
/// Асинхронна версія узагальненого CRUD-сервісу.
/// ЛР2: реалізація зберігала дані в пам'яті та у файлі (CrudServiceAsync&lt;T&gt;).
/// ЛР3: інтерфейс оновлено за завданням (без IEnumerable&lt;T&gt;); нова реалізація
/// CrudServiceAsync&lt;TDomain, TModel&gt; працює з базою даних через репозиторій.
/// </summary>
public interface ICrudServiceAsync<T>
{
    public Task<bool> CreateAsync(T element);

    public Task<T> ReadAsync(Guid id);

    public Task<IEnumerable<T>> ReadAllAsync();

    public Task<IEnumerable<T>> ReadAllAsync(int page, int amount);

    public Task<bool> UpdateAsync(T element);

    public Task<bool> RemoveAsync(T element);

    public Task<bool> SaveAsync();
}
