using GameStudio.Common.Interfaces;
using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStudio.Infrastructure.Repositories;

/// <summary>
/// ЛР3: узагальнена реалізація шаблону «Репозиторій» на основі EF Core.
/// Використовує <see cref="GameStudioContext"/> для доступу до БД; один клас обслуговує всі моделі
/// (EmployeeModel, GameProjectModel, BugReportModel), бо працює з DbSet&lt;T&gt;.
/// AddAsync / Update / Delete лише реєструють зміни в контексті, а SaveChangesAsync зберігає їх однією транзакцією.
/// </summary>
public class Repository<T> : IEntityRepository<T>
    where T : class, IDbModel
{
    // конструктор — контекст передається ззовні
    public Repository(GameStudioContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Set = context.Set<T>();
    }

    protected GameStudioContext Context { get; }

    protected DbSet<T> Set { get; }

    // метод — запис за первинним ключем або KeyNotFoundException
    public async Task<T> GetByIdAsync(int id)
    {
        return await Set.FirstOrDefaultAsync(entity => entity.Id == id)
               ?? throw new KeyNotFoundException($"Запис {typeof(T).Name} з Id {id} не знайдено.");
    }

    // метод — усі записи таблиці, упорядковані за первинним ключем
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Set.OrderBy(entity => entity.Id).ToListAsync();
    }

    // метод — додає запис у контекст (INSERT виконається під час SaveChangesAsync)
    public async Task AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Set.AddAsync(entity);
    }

    // метод — позначає запис як змінений (UPDATE виконається під час SaveChangesAsync)
    public Task Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // відстежувані сутності EF Core перевіряє сам; Update потрібен лише для сутностей, отриманих поза контекстом
        if (Context.Entry(entity).State == EntityState.Detached)
        {
            Set.Update(entity);
        }

        return Task.CompletedTask;
    }

    // метод — позначає запис для видалення (DELETE виконається під час SaveChangesAsync)
    public Task Delete(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(entity);
        return Task.CompletedTask;
    }

    // метод — пошук за альтернативним ключем PublicId: спершу серед доданих, але ще не збережених записів, потім у БД
    public async Task<T?> GetByPublicIdAsync(Guid publicId)
    {
        return Set.Local.FirstOrDefault(entity => entity.PublicId == publicId)
               ?? await Set.FirstOrDefaultAsync(entity => entity.PublicId == publicId);
    }

    // метод — пагінація на рівні БД (SQL LIMIT / OFFSET)
    public async Task<IEnumerable<T>> GetPageAsync(int page, int amount)
    {
        return await Set
            .OrderBy(entity => entity.Id)
            .Skip((page - 1) * amount)
            .Take(amount)
            .ToListAsync();
    }

    public Task<int> CountAsync() => Set.CountAsync();

    public Task<int> SaveChangesAsync() => Context.SaveChangesAsync();
}
