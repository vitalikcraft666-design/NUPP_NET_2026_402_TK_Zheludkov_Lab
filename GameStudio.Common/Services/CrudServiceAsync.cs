using System.Collections;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using GameStudio.Common.Interfaces;

namespace GameStudio.Common.Services;

/// <summary>
/// ЛР2: асинхронний, багатопотоково-безпечний (thread-safe) узагальнений CRUD-сервіс.
/// <list type="bullet">
/// <item>дані зберігаються у потокобезпечній колекції <see cref="ConcurrentDictionary{TKey,TValue}"/>;</item>
/// <item>порядок додавання запам'ятовується лічильником, який збільшується атомарно (<see cref="Interlocked"/>), —
/// це потрібно для стабільної пагінації;</item>
/// <item>збереження у файл <see cref="FilePath"/> виконується асинхронно і захищене <see cref="SemaphoreSlim"/>,
/// тому одночасні виклики <see cref="SaveAsync"/> не зіпсують файл.</item>
/// </list>
/// </summary>
public class CrudServiceAsync<T> : ICrudServiceAsync<T>
    where T : class, IEntity
{
    // статичне поле — спільні налаштування серіалізації
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters = { new JsonStringEnumConverter() }
    };

    // статичне поле — по одному семафору на кожен файл, спільному для всіх екземплярів сервісу в процесі:
    // навіть два різні сервіси з однаковим FilePath не писатимуть у файл одночасно
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    // потокобезпечна вбудована колекція .NET: ключ — Id, значення — елемент разом із його порядковим номером
    private readonly ConcurrentDictionary<Guid, StoredItem> _items = new();

    // семафор (1 дозвіл) для доступу до файлу цього сервісу
    private readonly SemaphoreSlim _fileLock;

    // лічильник порядку додавання; змінюється тільки через Interlocked
    private long _sequence;

    // конструктор
    public CrudServiceAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
        _fileLock = FileLocks.GetOrAdd(Path.GetFullPath(filePath), _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>Шлях до файлу, у який сервіс зберігає колекцію.</summary>
    public string FilePath { get; }

    /// <summary>Поточна кількість елементів.</summary>
    public int Count => _items.Count;

    // метод — додає елемент; false, якщо елемент з таким Id уже є
    public Task<bool> CreateAsync(T element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (element.Id == Guid.Empty)
        {
            element.Id = Guid.NewGuid();
        }

        long order = Interlocked.Increment(ref _sequence);
        bool added = _items.TryAdd(element.Id, new StoredItem(order, element));

        // операція в пам'яті завершується одразу, тому повертаємо вже завершену задачу без зайвого перемикання потоків
        return Task.FromResult(added);
    }

    // метод — повертає елемент за Id або кидає KeyNotFoundException
    public Task<T> ReadAsync(Guid id)
    {
        if (_items.TryGetValue(id, out StoredItem stored))
        {
            return Task.FromResult(stored.Value);
        }

        return Task.FromException<T>(new KeyNotFoundException($"Елемент з Id {id} не знайдено."));
    }

    // метод — знімок (копія) усіх елементів у порядку додавання
    public Task<IEnumerable<T>> ReadAllAsync()
    {
        return Task.FromResult<IEnumerable<T>>(GetSnapshot());
    }

    // метод — пагінація: сторінка page (нумерація з 1) розміром amount елементів
    public Task<IEnumerable<T>> ReadAllAsync(int page, int amount)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Номер сторінки має бути не менше 1.");
        }

        if (amount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Розмір сторінки має бути не менше 1.");
        }

        long skip = (long)(page - 1) * amount;
        List<T> pageItems = skip >= int.MaxValue
            ? new List<T>()
            : GetSnapshot().Skip((int)skip).Take(amount).ToList();

        return Task.FromResult<IEnumerable<T>>(pageItems);
    }

    // метод — кількість сторінок для заданого розміру сторінки
    public int GetTotalPages(int amount)
    {
        if (amount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Розмір сторінки має бути не менше 1.");
        }

        return (Count + amount - 1) / amount;
    }

    // метод — замінює елемент з тим самим Id; false, якщо такого елемента немає
    public Task<bool> UpdateAsync(T element)
    {
        ArgumentNullException.ThrowIfNull(element);

        // TryUpdate спрацює лише якщо ніхто не змінив елемент між читанням і записом;
        // інакше повторюємо спробу (оптимістичне блокування без lock)
        while (_items.TryGetValue(element.Id, out StoredItem current))
        {
            if (_items.TryUpdate(element.Id, current with { Value = element }, current))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    // метод — видаляє елемент; false, якщо його не було
    public Task<bool> RemoveAsync(T element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return Task.FromResult(_items.TryRemove(element.Id, out _));
    }

    // метод — асинхронно серіалізує колекцію у файл FilePath
    public async Task<bool> SaveAsync()
    {
        // асинхронне очікування семафора: потік не блокується, поки інший виклик пише файл
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            string fullPath = Path.GetFullPath(FilePath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // спочатку пишемо у тимчасовий файл, потім підміняємо основний:
            // якщо запис перерветься, старий файл залишиться цілим
            string tempPath = fullPath + ".tmp";
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, GetSnapshot(), JsonOptions).ConfigureAwait(false);
            }

            File.Move(tempPath, fullPath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    // додатковий метод — асинхронно завантажує колекцію з файлу FilePath (поточний вміст замінюється)
    public async Task<bool> LoadAsync()
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(FilePath))
            {
                return false;
            }

            List<T>? elements;
            await using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                elements = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions).ConfigureAwait(false);
            }

            _items.Clear();
            foreach (T element in elements ?? new List<T>())
            {
                long order = Interlocked.Increment(ref _sequence);
                _items[element.Id] = new StoredItem(order, element);
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    // реалізація IEnumerable<T>: перебір іде по знімку, тому паралельні зміни не кидають InvalidOperationException
    public IEnumerator<T> GetEnumerator() => GetSnapshot().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // метод — копія елементів, упорядкована за порядком додавання
    private List<T> GetSnapshot()
    {
        return _items.Values
            .OrderBy(stored => stored.Order)
            .Select(stored => stored.Value)
            .ToList();
    }

    // елемент колекції + його порядковий номер (record struct — значущий тип із порівнянням за значенням)
    private readonly record struct StoredItem(long Order, T Value);
}
