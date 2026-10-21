using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStudio.Infrastructure;

/// <summary>
/// Контекст бази даних ігрової студії. Описує схему БД: набори сутностей (DbSet) і,
/// через класи конфігурації з папки Configurations, таблиці, ключі, обмеження та зв'язки (Fluent API).
/// </summary>
public class GameStudioContext : DbContext
{
    // конструктор — параметри підключення (провайдер, рядок підключення) передаються ззовні
    public GameStudioContext(DbContextOptions<GameStudioContext> options)
        : base(options)
    {
    }

    public DbSet<EmployeeModel> Employees { get; set; } = null!;

    public DbSet<ProgrammerModel> Programmers { get; set; } = null!;

    public DbSet<ArtistModel> Artists { get; set; } = null!;

    public DbSet<GameProjectModel> GameProjects { get; set; } = null!;

    public DbSet<StorePageModel> StorePages { get; set; } = null!;

    public DbSet<BugReportModel> BugReports { get; set; } = null!;

    public DbSet<ProjectAssignmentModel> ProjectAssignments { get; set; } = null!;

    // статичний метод — контекст для файлу SQLite за вказаним шляхом
    public static GameStudioContext CreateSqlite(string databasePath)
    {
        DbContextOptions<GameStudioContext> options = new DbContextOptionsBuilder<GameStudioContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new GameStudioContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // підключаємо всі класи IEntityTypeConfiguration<T> з цієї збірки (папка Configurations)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameStudioContext).Assembly);
    }
}
