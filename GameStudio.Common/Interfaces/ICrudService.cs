namespace GameStudio.Common.Interfaces;

/// <summary>
/// Узагальнений (generic) інтерфейс CRUD-сервісу: Create, Read, Update, Delete.
/// </summary>
public interface ICrudService<T>
{
    public void Create(T element);

    public T Read(Guid id);

    public IEnumerable<T> ReadAll();

    public void Update(T element);

    public void Remove(T element);
}
