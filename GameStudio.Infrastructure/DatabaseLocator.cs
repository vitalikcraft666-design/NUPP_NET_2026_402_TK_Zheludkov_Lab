namespace GameStudio.Infrastructure;

/// <summary>
/// Визначає шлях до файлу бази даних SQLite. Файл лежить у папці Database поруч із GameStudio.sln
/// і зберігається в git-репозиторії, тому і консольний застосунок, і команди dotnet ef працюють з одним файлом.
/// </summary>
public static class DatabaseLocator
{
    public const string DatabaseFileName = "gamestudio.db";

    // статичний метод — шлях до БД: шукаємо папку з GameStudio.sln вгору від поточної папки та папки програми
    public static string GetDefaultDatabasePath()
    {
        string root = FindSolutionDirectory(Directory.GetCurrentDirectory())
                      ?? FindSolutionDirectory(AppContext.BaseDirectory)
                      ?? Directory.GetCurrentDirectory();

        string databaseDirectory = Path.Combine(root, "Database");
        Directory.CreateDirectory(databaseDirectory);
        return Path.Combine(databaseDirectory, DatabaseFileName);
    }

    private static string? FindSolutionDirectory(string startDirectory)
    {
        for (DirectoryInfo? directory = new(startDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GameStudio.sln")))
            {
                return directory.FullName;
            }
        }

        return null;
    }
}
