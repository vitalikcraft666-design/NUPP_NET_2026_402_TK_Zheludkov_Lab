using GameStudio.Common.Models;
using GameStudio.Infrastructure;
using GameStudio.Infrastructure.Models;
using GameStudio.Nosql;
using GameStudio.Nosql.Documents;
using GameStudio.Nosql.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using static GameStudio.ConsoleApp.ConsoleUi;

namespace GameStudio.ConsoleApp.Demos;

/// <summary>
/// Додаткове завдання ЛР3: репозиторій для нереляційної БД MongoDB.
/// Дані беруться з реляційної БД (SQLite), перетворюються на документи і записуються через MongoRepository.
/// Запуск: dotnet run --project GameStudio.Console -- nosql
/// </summary>
internal static class NosqlDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("Лабораторна робота №3, додаткове завдання — MongoDB (GameStudio.Nosql)");
        Console.WriteLine($"Запуск: {DateTime.Now:dd.MM.yyyy HH:mm}");

        PrintSection("1. Підключення до MongoDB");
        string connectionString = GameStudioMongoContext.GetConnectionString();
        var mongo = new GameStudioMongoContext(connectionString);
        try
        {
            Console.WriteLine($"Рядок підключення: {connectionString}; база даних: {mongo.DatabaseName}");
            Console.WriteLine($"Сервер MongoDB, версія: {await mongo.GetServerVersionAsync()}");
        }
        catch (TimeoutException)
        {
            Console.WriteLine("MongoDB недоступна. Запустіть сервер (mongod) або вкажіть рядок підключення у змінній GAMESTUDIO_MONGO.");
            return;
        }

        // база перезаповнюється при кожному запуску, щоб результат був відтворюваним
        await mongo.DropDatabaseAsync();
        await mongo.CreateIndexesAsync();
        MongoRepository<EmployeeDocument> employees = mongo.CreateEmployeeRepository();
        MongoRepository<GameProjectDocument> projects = mongo.CreateProjectRepository();

        await CopyFromSqliteAsync(employees, projects);
        await DemonstrateRepositoryAsync(employees, projects);
        await DemonstrateQueriesAsync(mongo, projects);
    }

    // 2. Перенесення даних із реляційної БД у документну
    private static async Task CopyFromSqliteAsync(MongoRepository<EmployeeDocument> employees, MongoRepository<GameProjectDocument> projects)
    {
        PrintSection("2. Перенесення даних із SQLite (7 таблиць) у MongoDB (2 колекції) через MongoRepository.AddAsync");

        (List<EmployeeModel> employeeModels, List<GameProjectModel> projectModels) =
            await RelationalSnapshot.LoadAsync(DatabaseLocator.GetDefaultDatabasePath());

        foreach (EmployeeModel model in employeeModels)
        {
            await employees.AddAsync(ToDocument(model));
        }

        foreach (GameProjectModel model in projectModels)
        {
            await projects.AddAsync(ToDocument(model));
        }

        Console.WriteLine($"Колекція employees: {await employees.CountAsync()} документів (Programmer і Artist в одній колекції, тип — у полі _t)");
        Console.WriteLine($"Колекція projects:  {await projects.CountAsync()} документів " +
                          $"(вкладено: сторінок — {projectModels.Count(p => p.StorePage is not null)}, " +
                          $"учасників команд — {projectModels.Sum(p => p.Assignments.Count)}, багів — {projectModels.Sum(p => p.Bugs.Count)})");
    }

    // 3. Операції IRepository<T> над MongoDB
    private static async Task DemonstrateRepositoryAsync(MongoRepository<EmployeeDocument> employees, MongoRepository<GameProjectDocument> projects)
    {
        PrintSection("3. IRepository<T> для MongoDB: GetByIdAsync / GetAllAsync / AddAsync / Update / Delete");

        GameProjectDocument project = (await projects.GetAllAsync()).OrderByDescending(p => p.Bugs.Count).ThenBy(p => p.Id).First();
        GameProjectDocument byId = await projects.GetByIdAsync(project.Id);
        Console.WriteLine($"GetByIdAsync({byId.Id}) — проєкт «{byId.Title}» з усіма вкладеними даними (один документ, без JOIN):");
        foreach (string line in byId.ToJson(new JsonWriterSettings { Indent = true }).Split('\n'))
        {
            Console.WriteLine($"  {line.TrimEnd()}");
        }

        var newcomer = new ProgrammerDocument
        {
            PublicId = Guid.NewGuid(),
            FullName = "Тестовий Програміст",
            HireDate = DateTime.Today,
            BaseSalary = 30_000m,
            PrimaryLanguage = "C#",
            Grade = ProgrammerGrade.Junior
        };
        await employees.AddAsync(newcomer);
        Console.WriteLine();
        Console.WriteLine($"AddAsync(новий програміст з Id = 0) → отримав _id = {newcomer.Id} (лічильник у колекції counters)");

        newcomer.Grade = ProgrammerGrade.Middle;
        newcomer.CommitsCount = 15;
        await employees.Update(newcomer);
        var updated = (ProgrammerDocument)await employees.GetByIdAsync(newcomer.Id);
        Console.WriteLine($"Update → GetByIdAsync({newcomer.Id}): {updated.FullName}, {updated.Grade}, комітів: {updated.CommitsCount}");

        await employees.Delete(newcomer);
        try
        {
            await employees.GetByIdAsync(newcomer.Id);
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"Delete → GetByIdAsync({newcomer.Id}) → {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine($"GetAllAsync: працівників — {(await employees.GetAllAsync()).Count()}, проєктів — {(await projects.GetAllAsync()).Count()}");
    }

    // 4. Запити до вкладених документів і агрегація на сервері MongoDB
    private static async Task DemonstrateQueriesAsync(GameStudioMongoContext mongo, MongoRepository<GameProjectDocument> projects)
    {
        PrintSection("4. Запити до вкладених документів і агрегація на сервері MongoDB");

        IEnumerable<GameProjectDocument> critical = await projects.FindAsync(
            p => p.Bugs.Any(b => b.Severity == BugSeverity.Critical && !b.IsFixed));
        Console.WriteLine($"Проєкти з невиправленими критичними багами (фільтр по масиву Bugs): {critical.Count()}");
        foreach (GameProjectDocument project in critical.Take(3))
        {
            Console.WriteLine($"  _id {project.Id,3}: «{project.Title}» — критичних відкритих: " +
                              $"{project.Bugs.Count(b => b.Severity == BugSeverity.Critical && !b.IsFixed)}");
        }

        var byStatus = await mongo.Projects.AsQueryable()
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), AverageBudget = g.Average(p => p.Budget) })
            .OrderBy(g => g.Status)
            .ToListAsync();

        Console.WriteLine();
        Console.WriteLine("Проєкти за статусом (конвеєр агрегації $group на сервері):");
        foreach (var group in byStatus)
        {
            Console.WriteLine($"  {group.Status,-11} проєктів: {group.Count,3}, середній бюджет: {group.AverageBudget,14:N0} грн");
        }

        EmployeeDocument? veteran = await mongo.Employees.Find(e => e is ProgrammerDocument)
            .SortBy(e => e.HireDate)
            .FirstOrDefaultAsync();
        Console.WriteLine();
        Console.WriteLine($"Фільтр за типом (поле _t = \"Programmer\"), найдавніше прийнятий: {veteran?.FullName}, з {veteran?.HireDate:dd.MM.yyyy}");
        Console.WriteLine("Роботу програми завершено.");
    }

    private static EmployeeDocument ToDocument(EmployeeModel model)
    {
        EmployeeDocument document = model switch
        {
            ProgrammerModel p => new ProgrammerDocument { PrimaryLanguage = p.PrimaryLanguage, Grade = p.Grade, CommitsCount = p.CommitsCount },
            ArtistModel a => new ArtistDocument { Specialization = a.Specialization, MainTool = a.MainTool, ArtworksCompleted = a.ArtworksCompleted },
            _ => throw new NotSupportedException(model.GetType().Name)
        };

        document.Id = model.Id;
        document.PublicId = model.PublicId;
        document.FullName = model.FullName;
        document.HireDate = model.HireDate;
        document.BaseSalary = model.BaseSalary;
        return document;
    }

    private static GameProjectDocument ToDocument(GameProjectModel model)
    {
        return new GameProjectDocument
        {
            Id = model.Id,
            PublicId = model.PublicId,
            Title = model.Title,
            Genre = model.Genre,
            Engine = model.Engine,
            Budget = model.Budget,
            Spent = model.Spent,
            Status = model.Status,
            StorePage = model.StorePage is null
                ? null
                : new StorePageDocument
                {
                    Url = model.StorePage.Url,
                    Price = model.StorePage.Price,
                    WishlistCount = model.StorePage.WishlistCount,
                    ReleaseDate = model.StorePage.ReleaseDate
                },
            Team = model.Assignments
                .OrderBy(a => a.EmployeeId)
                .Select(a => new TeamMemberDocument { EmployeeId = a.EmployeeId, FullName = a.Employee.FullName, Role = a.Role })
                .ToList(),
            Bugs = model.Bugs
                .OrderBy(b => b.Id)
                .Select(b => new BugDocument
                {
                    BugId = b.Id,
                    Title = b.Title,
                    Severity = b.Severity,
                    IsFixed = b.IsFixed,
                    CreatedAt = b.CreatedAt,
                    AssigneeId = b.AssigneeId,
                    AssigneeName = b.Assignee?.FullName
                })
                .ToList()
        };
    }
}
