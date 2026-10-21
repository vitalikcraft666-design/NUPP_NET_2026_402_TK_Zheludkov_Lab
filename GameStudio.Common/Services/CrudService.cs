using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using GameStudio.Common.Interfaces;

namespace GameStudio.Common.Services;

/// <summary>
/// Узагальнений CRUD-сервіс. Дані зберігаються у вбудованій колекції .NET — Dictionary&lt;Guid, T&gt;,
/// де ключ — це Id елемента. Обмеження where T : class, IEntity гарантує, що в елемента є Id.
/// </summary>
public class CrudService<T> : IPersistentCrudService<T>
    where T : class, IEntity
{
    // статичне поле — спільні налаштування серіалізації для всіх екземплярів CrudService<T>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters = { new JsonStringEnumConverter() }
    };

    // вбудована колекція .NET, у якій сервіс зберігає дані
    private readonly Dictionary<Guid, T> _storage = new();

    // події сервісу на основі вбудованого делегата Action<T>
    public event Action<T>? ElementCreated;

    public event Action<T>? ElementUpdated;

    public event Action<T>? ElementRemoved;

    public int Count => _storage.Count;

    // метод — додає новий елемент
    public void Create(T element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (element.Id == Guid.Empty)
        {
            element.Id = Guid.NewGuid();
        }

        if (!_storage.TryAdd(element.Id, element))
        {
            throw new InvalidOperationException($"Елемент з Id {element.Id} уже існує.");
        }

        ElementCreated?.Invoke(element);
    }

    // метод — повертає елемент за Id або кидає KeyNotFoundException
    public T Read(Guid id)
    {
        if (_storage.TryGetValue(id, out T? element))
        {
            return element;
        }

        throw new KeyNotFoundException($"Елемент з Id {id} не знайдено.");
    }

    // метод — повертає копію всіх елементів (щоб зовнішній код не змінив внутрішню колекцію)
    public IEnumerable<T> ReadAll()
    {
        return _storage.Values.ToList();
    }

    // метод — замінює збережений елемент новою версією з тим самим Id
    public void Update(T element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!_storage.ContainsKey(element.Id))
        {
            throw new KeyNotFoundException($"Неможливо оновити: елемент з Id {element.Id} не знайдено.");
        }

        _storage[element.Id] = element;
        ElementUpdated?.Invoke(element);
    }

    // метод — видаляє елемент
    public void Remove(T element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!_storage.Remove(element.Id))
        {
            throw new KeyNotFoundException($"Неможливо видалити: елемент з Id {element.Id} не знайдено.");
        }

        ElementRemoved?.Invoke(element);
    }

    // метод — пошук за умовою; умова передається як вбудований делегат Func<T, bool>
    public IEnumerable<T> Find(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _storage.Values.Where(predicate).ToList();
    }

    // Додаткове завдання: метод — зберігає всі елементи у файл у форматі JSON
    public void Save(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(_storage.Values.ToList(), JsonOptions);
        File.WriteAllText(filePath, json);
    }

    // Додаткове завдання: метод — завантажує елементи з JSON-файлу (поточний вміст сервісу замінюється)
    public void Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл з даними не знайдено.", filePath);
        }

        string json = File.ReadAllText(filePath);
        List<T> elements = JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();

        _storage.Clear();
        foreach (T element in elements)
        {
            _storage[element.Id] = element;
        }
    }
}
