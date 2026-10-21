namespace GameStudio.Common.Interfaces;

/// <summary>
/// ЛР2: асинхронна версія узагальненого CRUD-сервісу.
/// Наслідує IEnumerable&lt;T&gt;, тому сервіс можна перебирати через foreach і використовувати з LINQ.
/// </summary>
public interface ICrudServiceAsync<T> : IEnumerable<T>
{
    public Task<bool> CreateAsync(T element);

    public Task<T> ReadAsync(Guid id);

    public Task<IEnumerable<T>> ReadAllAsync();

    public Task<IEnumerable<T>> ReadAllAsync(int page, int amount);

    public Task<bool> UpdateAsync(T element);

    public Task<bool> RemoveAsync(T element);

    public Task<bool> SaveAsync();
}
