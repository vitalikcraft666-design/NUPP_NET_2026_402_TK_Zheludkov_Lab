using System.Collections.Concurrent;
using System.Diagnostics;
using GameStudio.Common.Models;
using GameStudio.Common.Services;
using GameStudio.Infrastructure;
using GameStudio.Infrastructure.Mapping;
using GameStudio.Infrastructure.Models;
using GameStudio.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using static GameStudio.ConsoleApp.ConsoleUi;

namespace GameStudio.ConsoleApp.Demos;

/// <summary>
/// Демонстрація лабораторної роботи №3: EF Core + SQLite, міграції, репозиторій,
/// CRUD-сервіс поверх репозиторію, зв'язки 1:1, 1:N, N:M і наслідування TPT.
/// </summary>
internal static class Lab3Demo
{
    private const int EmployeesCount = 200;
    private const int ProjectsCount = 60;

    public static async Task RunAsync(bool reset)
    {
        Console.WriteLine("Лабораторна робота №3 — Entity Framework Core, SQLite, шаблон «Репозиторій» (GameStudio)");
        Console.WriteLine($"Запуск: {DateTime.Now:dd.MM.yyyy HH:mm}");

        string databasePath = DatabaseLocator.GetDefaultDatabasePath();
        await PrepareDatabaseAsync(databasePath, reset);

        // окремий DbContext для кожного сервісу: контекст не потокобезпечний і відстежує «свої» сутності
        await using (GameStudioContext employeeContext = GameStudioContext.CreateSqlite(databasePath))
        await using (GameStudioContext projectContext = GameStudioContext.CreateSqlite(databasePath))
        await using (GameStudioContext bugContext = GameStudioContext.CreateSqlite(databasePath))
        {
            var services = new Services(employeeContext, projectContext, bugContext);

            if (await services.Employees.CountAsync() == 0)
            {
                await SeedAsync(services);
            }
            else
            {
                PrintSection("2. Заповнення БД");
                Console.WriteLine("БД уже містить дані — генерацію пропущено (для повторного заповнення запустіть з аргументом --reset).");
            }
        }

        await ReadFromDatabaseAsync(databasePath);
        await DemonstrateCrudAsync(databasePath);
        await DemonstrateRelationshipsAsync(databasePath);
        await DemonstrateServerSideQueriesAsync(databasePath);
        await PrintSummaryAsync(databasePath);
    }

    // 1. Створення / оновлення схеми БД за міграціями
    private static async Task PrepareDatabaseAsync(string databasePath, bool reset)
    {
        PrintSection("1. База даних SQLite і міграції EF Core");
        await using GameStudioContext context = GameStudioContext.CreateSqlite(databasePath);

        if (reset)
        {
            await context.Database.EnsureDeletedAsync();
            Console.WriteLine("Аргумент --reset: файл БД видалено, схему буде створено заново.");
        }

        IEnumerable<string> pending = await context.Database.GetPendingMigrationsAsync();
        Console.WriteLine($"Міграції, що очікують застосування: {FormatList(pending)}");

        // застосовує всі незастосовані міграції (те саме, що dotnet ef database update)
        await context.Database.MigrateAsync();

        // режим журналу DELETE — БД зберігається одним файлом (без -wal/-shm), зручно для git
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = DELETE;");

        Console.WriteLine($"Файл БД: {Path.GetRelativePath(Path.GetDirectoryName(Path.GetDirectoryName(databasePath)!)!, databasePath)}");
        Console.WriteLine($"Застосовані міграції: {FormatList(await context.Database.GetAppliedMigrationsAsync())}");

        List<string> tables = await context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
            .ToListAsync();
        Console.WriteLine($"Таблиці: {string.Join(", ", tables)}");
    }

    // 2. Заповнення БД згенерованими даними через CRUD-сервіси
    private static async Task SeedAsync(Services services)
    {
        PrintSection("2. Заповнення БД: Parallel + CreateNew → CrudServiceAsync → репозиторій → SQLite");
        var stopwatch = Stopwatch.StartNew();

        // 2.1 Працівники: об'єкти генеруються паралельно, CreateAsync викликається одночасно з багатьох задач —
        // сервіс сам впорядковує доступ до DbContext семафором
        var employees = new Employee[EmployeesCount];
        Parallel.For(0, EmployeesCount, i =>
            employees[i] = Random.Shared.Next(2) == 0 ? Programmer.CreateNew() : Artist.CreateNew());

        bool[] employeesCreated = await Task.WhenAll(employees.Select(services.Employees.CreateAsync));
        bool employeesSaved = await services.Employees.SaveAsync();
        Console.WriteLine($"Працівники: додано {employeesCreated.Count(ok => ok)} з {EmployeesCount}, SaveAsync → {employeesSaved} " +
                          $"(програмістів: {employees.OfType<Programmer>().Count()}, художників: {employees.OfType<Artist>().Count()})");

        // 2.2 Проєкти: команда (зв'язок багато-до-багатьох) — 3–8 реальних працівників із БД
        var projects = new GameProject[ProjectsCount];
        Parallel.For(0, ProjectsCount, i =>
        {
            GameProject project = GameProject.CreateNew();
            project.TeamMemberIds.Clear();
            foreach (Employee member in employees.OrderBy(_ => Random.Shared.Next()).Take(Random.Shared.Next(3, 9)))
            {
                project.AddTeamMember(member);
            }

            projects[i] = project;
        });

        bool[] projectsCreated = await Task.WhenAll(projects.Select(services.Projects.CreateAsync));
        bool projectsSaved = await services.Projects.SaveAsync();
        Console.WriteLine($"Проєкти: додано {projectsCreated.Count(ok => ok)} з {ProjectsCount}, SaveAsync → {projectsSaved} " +
                          $"(зі сторінкою в магазині: {projects.Count(p => p.StorePage is not null)}, " +
                          $"призначень у команди: {projects.Sum(p => p.TeamMemberIds.Count)})");

        // 2.3 Баги: 1–4 на проєкт; виконавець — програміст із команди проєкту (якщо є)
        var programmersById = employees.OfType<Programmer>().ToDictionary(p => p.Id);
        var bugs = new ConcurrentBag<BugReport>();
        Parallel.ForEach(projects, project =>
        {
            Programmer[] teamProgrammers = project.TeamMemberIds
                .Where(programmersById.ContainsKey)
                .Select(id => programmersById[id])
                .ToArray();

            int bugsCount = Random.Shared.Next(1, 5);
            for (int i = 0; i < bugsCount; i++)
            {
                BugReport bug = BugReport.CreateNew(project.Id);
                bool assigned = teamProgrammers.Length > 0 && Random.Shared.Next(3) > 0;
                bug.AssigneeId = assigned ? teamProgrammers[Random.Shared.Next(teamProgrammers.Length)].Id : null;
                bug.IsFixed = assigned && Random.Shared.Next(2) == 0;
                bugs.Add(bug);
            }
        });

        bool[] bugsCreated = await Task.WhenAll(bugs.Select(services.Bugs.CreateAsync));
        bool bugsSaved = await services.Bugs.SaveAsync();
        Console.WriteLine($"Баги: додано {bugsCreated.Count(ok => ok)} з {bugs.Count}, SaveAsync → {bugsSaved} " +
                          $"(призначено виконавця: {bugs.Count(b => b.AssigneeId is not null)})");

        Console.WriteLine($"Час заповнення: {stopwatch.ElapsedMilliseconds} мс");
    }

    // 3. Читання даних з БД через оновлений CRUD-сервіс (нові контексти — дані беруться саме з файлу БД)
    private static async Task ReadFromDatabaseAsync(string databasePath)
    {
        PrintSection("3. Читання даних із БД через CrudServiceAsync<TDomain, TModel> (нові DbContext)");

        await using GameStudioContext employeeContext = GameStudioContext.CreateSqlite(databasePath);
        await using GameStudioContext projectContext = GameStudioContext.CreateSqlite(databasePath);
        await using GameStudioContext bugContext = GameStudioContext.CreateSqlite(databasePath);
        var services = new Services(employeeContext, projectContext, bugContext);

        List<Employee> employees = (await services.Employees.ReadAllAsync()).ToList();
        List<GameProject> projects = (await services.Projects.ReadAllAsync()).ToList();
        List<BugReport> bugs = (await services.Bugs.ReadAllAsync()).ToList();
        Console.WriteLine($"ReadAllAsync: працівників — {employees.Count} (Programmer: {employees.OfType<Programmer>().Count()}, " +
                          $"Artist: {employees.OfType<Artist>().Count()}), проєктів — {projects.Count}, багів — {bugs.Count}");

        int totalPages = await services.Employees.GetTotalPagesAsync(5);
        Console.WriteLine($"Пагінація працівників по 5 (LIMIT/OFFSET у SQL): сторінок — {totalPages}");
        foreach (int page in new[] { 1, 2 })
        {
            PrintAll($"Сторінка {page}:", (await services.Employees.ReadAllAsync(page, 5)).Select(e => e.ToString().Trim()));
        }

        PrintAll("Проєкти, сторінка 1 по 3:", await services.Projects.ReadAllAsync(1, 3));

        GameProject sample = projects.First(p => p.StorePage is not null);
        GameProject read = await services.Projects.ReadAsync(sample.Id);
        Console.WriteLine($"ReadAsync({read.Id}):");
        Console.WriteLine($"  {read}");
        Console.WriteLine($"  сторінка в магазині: {read.StorePage}");
        Console.WriteLine($"  команда: {read.TeamMemberIds.Count} осіб; багів у проєкті: {bugs.Count(b => b.ProjectId == read.Id)}");
    }

    // 4. Створення, оновлення, видалення
    private static async Task DemonstrateCrudAsync(string databasePath)
    {
        PrintSection("4. CreateAsync / UpdateAsync / RemoveAsync + SaveAsync (зміни потрапляють у БД лише після SaveAsync)");

        await using GameStudioContext employeeContext = GameStudioContext.CreateSqlite(databasePath);
        await using GameStudioContext projectContext = GameStudioContext.CreateSqlite(databasePath);
        await using GameStudioContext bugContext = GameStudioContext.CreateSqlite(databasePath);
        var services = new Services(employeeContext, projectContext, bugContext);

        // оновлення: підвищення програміста
        Programmer programmer = (await services.Employees.ReadAllAsync()).OfType<Programmer>().First(p => p.Grade != ProgrammerGrade.Lead);
        string before = programmer.Position;
        programmer.Promote();
        programmer.MakeCommits(10);
        bool updated = await services.Employees.UpdateAsync(programmer);
        bool saved = await services.Employees.SaveAsync();
        Employee reloaded = await ReadWithNewContextAsync(databasePath, programmer.Id);
        Console.WriteLine($"UpdateAsync → {updated}, SaveAsync → {saved}: {programmer.FullName}: {before} → {reloaded.Position} (прочитано новим контекстом)");

        // створення проєкту зі сторінкою, командою і багом, потім видалення з каскадом
        var project = new GameProject("Demo Arena", "Action RPG", "Unity", 250_000m);
        project.ChangeStatus(ProjectStatus.Released);
        project.StorePage = StorePage.CreateNew(project.Title, released: true);
        project.AddTeamMember(programmer);

        Console.WriteLine($"CreateAsync(новий проєкт «{project.Title}») → {await services.Projects.CreateAsync(project)}; " +
                          $"SaveAsync → {await services.Projects.SaveAsync()}");
        Console.WriteLine($"CreateAsync(той самий Id) → {await services.Projects.CreateAsync(project)} (дублікат)");

        BugReport bug = BugReport.CreateNew(project.Id);
        bug.AssigneeId = programmer.Id;
        Console.WriteLine($"CreateAsync(баг для нового проєкту) → {await services.Bugs.CreateAsync(bug)}; SaveAsync → {await services.Bugs.SaveAsync()}");
        Console.WriteLine($"Рядків для проєкту в БД до видалення: {await CountProjectRowsAsync(databasePath, project.Id)}");

        GameProject toRemove = await services.Projects.ReadAsync(project.Id);
        Console.WriteLine($"RemoveAsync → {await services.Projects.RemoveAsync(toRemove)}; SaveAsync → {await services.Projects.SaveAsync()}");
        Console.WriteLine($"Рядків для проєкту в БД після видалення: {await CountProjectRowsAsync(databasePath, project.Id)} " +
                          "(StorePages, ProjectAssignments і BugReports видалено каскадно — ON DELETE CASCADE)");

        try
        {
            await services.Projects.ReadAsync(project.Id);
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"ReadAsync(видалений Id)  → {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            await services.Bugs.CreateAsync(BugReport.CreateNew(Guid.NewGuid()));
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Баг з неіснуючим проєктом → {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            await services.Employees.ReadAllAsync(0, 5);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"ReadAllAsync(0, 5)       → {ex.GetType().Name}: {ex.Message}");
        }
    }

    // 5. Зв'язки між таблицями: Include / ThenInclude
    private static async Task DemonstrateRelationshipsAsync(string databasePath)
    {
        PrintSection("5. Зв'язки між таблицями: жадібне завантаження Include / ThenInclude");
        await using GameStudioContext context = GameStudioContext.CreateSqlite(databasePath);

        PrintSubsection("Один-до-одного: GameProjects ↔ StorePages (проєкт із найбільшим списком бажаного)");
        GameProjectModel popular = await context.GameProjects
            .Include(p => p.StorePage)
            .Where(p => p.StorePage != null)
            .OrderByDescending(p => p.StorePage!.WishlistCount)
            .FirstAsync();
        Console.WriteLine($"    «{popular.Title}» ({popular.Status}) → {popular.StorePage!.Url}");
        Console.WriteLine($"    ціна: {popular.StorePage.Price:N0} грн, у бажаному: {popular.StorePage.WishlistCount:N0}, вихід: {popular.StorePage.ReleaseDate:dd.MM.yyyy}");

        PrintSubsection("Один-до-багатьох: GameProjects → BugReports (проєкт із найбільшою кількістю багів)");
        GameProjectModel buggy = await context.GameProjects
            .Include(p => p.Bugs)
            .ThenInclude(b => b.Assignee)
            .OrderByDescending(p => p.Bugs.Count)
            .ThenBy(p => p.Id)
            .FirstAsync();
        Console.WriteLine($"    «{buggy.Title}» — багів: {buggy.Bugs.Count}");
        foreach (BugReportModel bug in buggy.Bugs.OrderBy(b => b.Id))
        {
            string assignee = bug.Assignee is null ? "не призначено" : bug.Assignee.FullName;
            Console.WriteLine($"      [{bug.Severity,-8}] {bug.Title} — {(bug.IsFixed ? "виправлено" : "відкрито")}, виконавець: {assignee}");
        }

        PrintSubsection("Багато-до-багатьох: GameProjects ↔ Employees через ProjectAssignments (команда того ж проєкту)");
        GameProjectModel team = await context.GameProjects
            .Include(p => p.TeamMembers)
            .FirstAsync(p => p.Id == buggy.Id);
        foreach (ProjectAssignmentModel assignment in team.Assignments.OrderBy(a => a.Role).ThenBy(a => a.Employee.FullName))
        {
            Console.WriteLine($"      {assignment.Employee.FullName,-24} {assignment.Role,-11} ({assignment.Employee.GetType().Name}), з {assignment.AssignedAt:dd.MM.yyyy}");
        }

        EmployeeModel busiest = await context.Employees
            .Include(e => e.Projects)
            .OrderByDescending(e => e.Projects.Count)
            .ThenBy(e => e.Id)
            .FirstAsync();
        Console.WriteLine($"    Зворотний бік зв'язку: {busiest.FullName} працює в {busiest.Projects.Count} проєктах: " +
                          string.Join(", ", busiest.Projects.Select(p => p.Title)));

        PrintSubsection("Один-до-багатьох: Programmers → BugReports (виконавець із найбільшою кількістю багів)");
        ProgrammerModel worker = await context.Programmers
            .Include(p => p.AssignedBugs)
            .OrderByDescending(p => p.AssignedBugs.Count)
            .ThenBy(p => p.Id)
            .FirstAsync();
        Console.WriteLine($"    {worker.FullName} ({worker.Grade} {worker.PrimaryLanguage}) — багів: {worker.AssignedBugs.Count}, " +
                          $"виправлено: {worker.AssignedBugs.Count(b => b.IsFixed)}");

        PrintSubsection("Наслідування «таблиця на тип» (TPT): SQL, який генерує EF Core для запиту до Programmers");
        string sql = context.Programmers.Where(p => p.Grade == ProgrammerGrade.Lead).OrderBy(p => p.FullName).ToQueryString();
        foreach (string line in sql.Split('\n'))
        {
            Console.WriteLine($"    {line.TrimEnd()}");
        }
    }

    // 6. Агрегація та групування на стороні БД
    private static async Task DemonstrateServerSideQueriesAsync(string databasePath)
    {
        PrintSection("6. Агрегація на стороні БД (LINQ to Entities → SQL)");
        await using GameStudioContext context = GameStudioContext.CreateSqlite(databasePath);

        PrintStatsHeader();
        PrintStatsRow("Базова ставка, грн",
            await context.Employees.MinAsync(e => e.BaseSalary), await context.Employees.MaxAsync(e => e.BaseSalary),
            await context.Employees.AverageAsync(e => e.BaseSalary));
        PrintStatsRow("Бюджет проєкту, грн",
            await context.GameProjects.MinAsync(p => p.Budget), await context.GameProjects.MaxAsync(p => p.Budget),
            await context.GameProjects.AverageAsync(p => p.Budget));
        PrintStatsRow("Коміти програміста",
            await context.Programmers.MinAsync(p => p.CommitsCount), await context.Programmers.MaxAsync(p => p.CommitsCount),
            await context.Programmers.AverageAsync(p => p.CommitsCount));

        var byStatus = context.GameProjects
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Budget = g.Sum(p => p.Budget) })
            .OrderBy(g => g.Status);

        Console.WriteLine();
        Console.WriteLine("Проєкти за статусом (GROUP BY у SQL):");
        foreach (var group in await byStatus.ToListAsync())
        {
            Console.WriteLine($"  {group.Status,-11} проєктів: {group.Count,3}, сумарний бюджет: {group.Budget,16:N0} грн");
        }

        Console.WriteLine("SQL цього запиту:");
        foreach (string line in byStatus.ToQueryString().Split('\n'))
        {
            Console.WriteLine($"    {line.TrimEnd()}");
        }

        var bugsBySeverity = await context.BugReports
            .GroupBy(b => b.Severity)
            .Select(g => new { Severity = g.Key, Open = g.Count(b => !b.IsFixed), Fixed = g.Count(b => b.IsFixed) })
            .OrderBy(g => g.Severity)
            .ToListAsync();
        Console.WriteLine();
        Console.WriteLine("Баги за критичністю: " + string.Join("; ",
            bugsBySeverity.Select(g => $"{g.Severity} — відкрито {g.Open}, виправлено {g.Fixed}")));
    }

    // 7. Підсумок: кількість рядків у таблицях
    private static async Task PrintSummaryAsync(string databasePath)
    {
        PrintSection("7. Підсумок: вміст таблиць БД");
        await using GameStudioContext context = GameStudioContext.CreateSqlite(databasePath);

        var counts = new (string Table, int Rows)[]
        {
            ("Employees", await context.Employees.CountAsync()),
            ("Programmers", await context.Programmers.CountAsync()),
            ("Artists", await context.Artists.CountAsync()),
            ("GameProjects", await context.GameProjects.CountAsync()),
            ("StorePages", await context.StorePages.CountAsync()),
            ("ProjectAssignments", await context.ProjectAssignments.CountAsync()),
            ("BugReports", await context.BugReports.CountAsync())
        };

        foreach ((string table, int rows) in counts)
        {
            Console.WriteLine($"  {table,-20} {rows,5} рядків");
        }

        Console.WriteLine($"Розмір файлу БД: {new FileInfo(databasePath).Length:N0} байт");
        Console.WriteLine("Роботу програми завершено.");
    }

    private static async Task<Employee> ReadWithNewContextAsync(string databasePath, Guid id)
    {
        await using GameStudioContext context = GameStudioContext.CreateSqlite(databasePath);
        var service = new CrudServiceAsync<Employee, EmployeeModel>(new Repository<EmployeeModel>(context), new EmployeeMapper());
        return await service.ReadAsync(id);
    }

    // кількість рядків, пов'язаних із проєктом (сам проєкт + сторінка + призначення + баги)
    private static async Task<int> CountProjectRowsAsync(string databasePath, Guid projectId)
    {
        await using GameStudioContext context = GameStudioContext.CreateSqlite(databasePath);
        int? id = await context.GameProjects.Where(p => p.PublicId == projectId).Select(p => (int?)p.Id).FirstOrDefaultAsync();
        if (id is null)
        {
            return await context.StorePages.CountAsync(s => s.GameProject.PublicId == projectId);
        }

        return 1
               + await context.StorePages.CountAsync(s => s.GameProjectId == id)
               + await context.ProjectAssignments.CountAsync(a => a.GameProjectId == id)
               + await context.BugReports.CountAsync(b => b.GameProjectId == id);
    }

    private static string FormatList(IEnumerable<string> items)
    {
        List<string> list = items.ToList();
        return list.Count == 0 ? "немає" : string.Join(", ", list);
    }

    // три CRUD-сервіси, кожен зі своїм контекстом, репозиторієм і перетворювачем
    private sealed class Services
    {
        public Services(GameStudioContext employeeContext, GameStudioContext projectContext, GameStudioContext bugContext)
        {
            Employees = new CrudServiceAsync<Employee, EmployeeModel>(
                new Repository<EmployeeModel>(employeeContext), new EmployeeMapper());
            Projects = new CrudServiceAsync<GameProject, GameProjectModel>(
                new Repository<GameProjectModel>(projectContext), new GameProjectMapper(projectContext));
            Bugs = new CrudServiceAsync<BugReport, BugReportModel>(
                new Repository<BugReportModel>(bugContext), new BugReportMapper(bugContext));
        }

        public CrudServiceAsync<Employee, EmployeeModel> Employees { get; }

        public CrudServiceAsync<GameProject, GameProjectModel> Projects { get; }

        public CrudServiceAsync<BugReport, BugReportModel> Bugs { get; }
    }
}
