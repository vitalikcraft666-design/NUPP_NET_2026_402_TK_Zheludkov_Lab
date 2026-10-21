namespace GameStudio.Common.Interfaces;

/// <summary>
/// Додаткове завдання: CRUD-сервіс, який вміє зберігати свої дані у файл
/// та завантажувати їх із файлу. Інтерфейс наслідується від ICrudService.
/// </summary>
public interface IPersistentCrudService<T> : ICrudService<T>
{
    /// <summary>Серіалізує всі елементи сервісу у файл за шляхом <paramref name="filePath"/>.</summary>
    public void Save(string filePath);

    /// <summary>Замінює вміст сервісу даними, десеріалізованими з файлу <paramref name="filePath"/>.</summary>
    public void Load(string filePath);
}
