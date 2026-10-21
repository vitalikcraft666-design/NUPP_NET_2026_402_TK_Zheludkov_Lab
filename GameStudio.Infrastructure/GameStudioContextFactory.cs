using Microsoft.EntityFrameworkCore.Design;

namespace GameStudio.Infrastructure;

/// <summary>
/// Фабрика контексту для інструментів EF Core під час розробки
/// (dotnet ef migrations add / dotnet ef database update).
/// </summary>
public class GameStudioContextFactory : IDesignTimeDbContextFactory<GameStudioContext>
{
    public GameStudioContext CreateDbContext(string[] args)
    {
        return GameStudioContext.CreateSqlite(DatabaseLocator.GetDefaultDatabasePath());
    }
}
