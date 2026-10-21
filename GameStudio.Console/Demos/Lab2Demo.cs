using System.Collections.Concurrent;
using System.Diagnostics;
using GameStudio.Common.Extensions;
using GameStudio.Common.Models;
using GameStudio.Common.Services;
using static GameStudio.ConsoleApp.ConsoleUi;

namespace GameStudio.ConsoleApp.Demos;

/// <summary>
/// Демонстрація лабораторної роботи №2: асинхронний потокобезпечний CRUD-сервіс,
/// паралельне створення об'єктів, статистика LINQ, пагінація, збереження у файл, примітиви синхронізації.
/// </summary>
internal static class Lab2Demo
{
    private const int EmployeesCount = 1000;
    private const int ProjectsCount = 1000;
    private const int PageSize = 5;

    public static async Task RunAsync()
    {
        Console.WriteLine("Лабораторна робота №2 — асинхронний потокобезпечний CRUD-сервіс (GameStudio)");
        Console.WriteLine($"Запуск: {DateTime.Now:dd.MM.yyyy HH:mm}; логічних процесорів: {Environment.ProcessorCount}");

        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "data", "lab2");
        var employeeService = new CrudServiceAsync<Employee>(Path.Combine(dataDirectory, "employees.json"));
        var projectService = new CrudServiceAsync<GameProject>(Path.Combine(dataDirectory, "projects.json"));
        var bugService = new CrudServiceAsync<BugReport>(Path.Combine(dataDirectory, "bugs.json"));

        await GenerateAsync(employeeService, projectService, bugService);
        await PrintStatisticsAsync(employeeService, projectService, bugService);
        await DemonstratePaginationAsync(employeeService);
        await DemonstrateCrudOperationsAsync(employeeService);
        await DemonstrateConcurrentAccessAsync(employeeService);
        await SaveAndLoadAsync(employeeService, projectService, bugService);
        await SyncPrimitivesDemo.RunAsync();

        PrintSection("Підсумок");
        Console.WriteLine($"Працівників у сервісі: {employeeService.Count}, проєктів: {projectService.Count}, багів: {bugService.Count}");
        Console.WriteLine($"Усього створено об'єктів Employee за час роботи (разом із завантаженими з файлу): {Employee.GetCreatedCount()}");
        Console.WriteLine("Роботу програми завершено.");
    }

    // 1. Паралельна генерація об'єктів
    private static async Task GenerateAsync(
        CrudServiceAsync<Employee> employeeService,
        CrudServiceAsync<GameProject> projectService,
        CrudServiceAsync<BugReport> bugService)
    {
        PrintSection("1. Паралельне створення об'єктів (клас Parallel + статичні методи CreateNew + CrudServiceAsync)");

        // ідентифікатори потоків, які брали участь у генерації (потокобезпечна колекція)
        var threadIds = new ConcurrentDictionary<int, byte>();
        int createdBefore = Employee.GetCreatedCount();
        var stopwatch = Stopwatch.StartNew();

        // 1.1 Parallel.For: кожна ітерація створює працівника і запускає CreateAsync;
        // задачі збираються в масив і потім очікуються разом через Task.WhenAll
        var employeeTasks = new Task<bool>[EmployeesCount];
        Parallel.For(0, EmployeesCount, i =>
        {
            threadIds.TryAdd(Environment.CurrentManagedThreadId, 0);
            Employee employee = Random.Shared.Next(2) == 0 ? Programmer.CreateNew() : Artist.CreateNew();
            employeeTasks[i] = employeeService.CreateAsync(employee);
        });
        bool[] employeeResults = await Task.WhenAll(employeeTasks);

        // 1.2 Parallel.ForEachAsync: тіло циклу — асинхронна лямбда з await
        int projectsAdded = 0;
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        await Parallel.ForEachAsync(Enumerable.Range(0, ProjectsCount), options, async (_, _) =>
        {
            threadIds.TryAdd(Environment.CurrentManagedThreadId, 0);
            if (await projectService.CreateAsync(GameProject.CreateNew()))
            {
                Interlocked.Increment(ref projectsAdded);
            }
        });

        // 1.3 Parallel.ForEach: для кожного проєкту — 1–2 баги
        GameProject[] projects = (await projectService.ReadAllAsync()).ToArray();
        var bugTasks = new ConcurrentBag<Task<bool>>();
        Parallel.ForEach(projects, project =>
        {
            threadIds.TryAdd(Environment.CurrentManagedThreadId, 0);
            int bugsForProject = Random.Shared.Next(1, 3);
            for (int i = 0; i < bugsForProject; i++)
            {
                bugTasks.Add(bugService.CreateAsync(BugReport.CreateNew(project.Id)));
            }
        });
        bool[] bugResults = await Task.WhenAll(bugTasks);
        stopwatch.Stop();

        Console.WriteLine($"Працівників (Parallel.For):         додано {employeeResults.Count(added => added)} з {EmployeesCount}");
        Console.WriteLine($"Проєктів (Parallel.ForEachAsync):   додано {projectsAdded} з {ProjectsCount}");
        Console.WriteLine($"Багів (Parallel.ForEach, 1–2 на проєкт): додано {bugResults.Count(added => added)} з {bugResults.Length}");
        Console.WriteLine($"Усього об'єктів: {employeeService.Count + projectService.Count + bugService.Count}; " +
                          $"задіяно потоків: {threadIds.Count}; час: {stopwatch.ElapsedMilliseconds} мс");
        Console.WriteLine($"Статичний лічильник Employee: +{Employee.GetCreatedCount() - createdBefore} " +
                          "(збігається з кількістю створених працівників завдяки Interlocked.Increment)");
    }

    // 2. Мінімальні, максимальні та середні значення (LINQ)
    private static async Task PrintStatisticsAsync(
        CrudServiceAsync<Employee> employeeService,
        CrudServiceAsync<GameProject> projectService,
        CrudServiceAsync<BugReport> bugService)
    {
        PrintSection("2. Статистика згенерованих даних: мінімальні, максимальні та середні значення (LINQ)");

        List<Employee> employees = (await employeeService.ReadAllAsync()).ToList();
        List<Programmer> programmers = employees.OfType<Programmer>().ToList();
        List<Artist> artists = employees.OfType<Artist>().ToList();
        List<GameProject> projects = (await projectService.ReadAllAsync()).ToList();
        List<BugReport> bugs = (await bugService.ReadAllAsync()).ToList();

        Console.WriteLine($"Працівники: {employees.Count} (програмістів: {programmers.Count}, художників: {artists.Count})");
        PrintStatsHeader();
        PrintStatsRow("Базова ставка, грн",
            employees.Min(e => e.BaseSalary), employees.Max(e => e.BaseSalary), employees.Average(e => e.BaseSalary));
        PrintStatsRow("Місячна зарплата, грн",
            employees.Min(e => e.CalculateMonthlySalary()), employees.Max(e => e.CalculateMonthlySalary()), employees.Average(e => e.CalculateMonthlySalary()));
        PrintStatsRow("Стаж, років",
            employees.Min(e => e.GetYearsOfExperience()), employees.Max(e => e.GetYearsOfExperience()), employees.Average(e => e.GetYearsOfExperience()));
        PrintStatsRow("Коміти (програмісти)",
            programmers.Min(p => p.CommitsCount), programmers.Max(p => p.CommitsCount), programmers.Average(p => p.CommitsCount));
        PrintStatsRow("Завершені роботи (художники)",
            artists.Min(a => a.ArtworksCompleted), artists.Max(a => a.ArtworksCompleted), artists.Average(a => a.ArtworksCompleted));

        Console.WriteLine();
        Console.WriteLine($"Проєкти: {projects.Count}");
        PrintStatsHeader();
        PrintStatsRow("Бюджет, грн",
            projects.Min(p => p.Budget), projects.Max(p => p.Budget), projects.Average(p => p.Budget));
        PrintStatsRow("Витрачено, грн",
            projects.Min(p => p.Spent), projects.Max(p => p.Spent), projects.Average(p => p.Spent));
        PrintStatsRow("Використання бюджету, %",
            projects.Min(p => p.BudgetUsagePercent()), projects.Max(p => p.BudgetUsagePercent()), projects.Average(p => p.BudgetUsagePercent()), "N1");
        PrintStatsRow("Розмір команди, осіб",
            projects.Min(p => p.TeamMemberIds.Count), projects.Max(p => p.TeamMemberIds.Count), projects.Average(p => p.TeamMemberIds.Count));

        Console.WriteLine();
        Console.WriteLine($"Баги: {bugs.Count}");
        var bugsPerProject = bugs.GroupBy(b => b.ProjectId).Select(group => group.Count()).ToList();
        PrintStatsHeader();
        PrintStatsRow("Вік бага, днів",
            bugs.Min(b => (DateTime.Today - b.CreatedAt).TotalDays), bugs.Max(b => (DateTime.Today - b.CreatedAt).TotalDays), bugs.Average(b => (DateTime.Today - b.CreatedAt).TotalDays));
        PrintStatsRow("Багів на один проєкт",
            bugsPerProject.Min(), bugsPerProject.Max(), bugsPerProject.Average(), "N2");

        // групування (GroupBy) і сортування (OrderBy) для розподілів
        Console.WriteLine();
        Console.WriteLine("Програмісти за грейдом: " + string.Join(", ",
            programmers.GroupBy(p => p.Grade).OrderBy(g => g.Key).Select(g => $"{g.Key} — {g.Count()}")));
        Console.WriteLine("Проєкти за статусом:    " + string.Join(", ",
            projects.GroupBy(p => p.Status).OrderBy(g => g.Key).Select(g => $"{g.Key} — {g.Count()}")));
        Console.WriteLine("Баги за критичністю:    " + string.Join(", ",
            bugs.GroupBy(b => b.Severity).OrderBy(g => g.Key).Select(g => $"{g.Key} — {g.Count()}")) +
            $"; виправлено: {bugs.Count(b => b.IsFixed) * 100.0 / bugs.Count:N1} %");
        Console.WriteLine("Найпопулярніші мови:    " + string.Join(", ",
            programmers.GroupBy(p => p.PrimaryLanguage).OrderByDescending(g => g.Count()).Take(3).Select(g => $"{g.Key} — {g.Count()}")));

        Console.WriteLine();
        PrintAll("Топ-3 проєкти за бюджетом (OrderByDescending + Take):",
            projects.OrderByDescending(p => p.Budget).Take(3));
    }

    // 3. Пагінація
    private static async Task DemonstratePaginationAsync(CrudServiceAsync<Employee> employeeService)
    {
        PrintSection($"3. Пагінація: ReadAllAsync(page, amount), розмір сторінки — {PageSize}");

        int totalPages = employeeService.GetTotalPages(PageSize);
        Console.WriteLine($"Працівників: {employeeService.Count}, сторінок по {PageSize}: {totalPages}");

        foreach (int page in new[] { 1, 2, totalPages })
        {
            IEnumerable<Employee> pageItems = await employeeService.ReadAllAsync(page, PageSize);
            PrintAll($"Сторінка {page}:", pageItems.Select(e => $"{e.ToShortString()}, {e.CalculateMonthlySalary():N2} грн"));
        }

        IEnumerable<Employee> beyond = await employeeService.ReadAllAsync(totalPages + 1, PageSize);
        Console.WriteLine($"Сторінка {totalPages + 1} (за межами): елементів — {beyond.Count()}");

        try
        {
            await employeeService.ReadAllAsync(0, PageSize);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"ReadAllAsync(0, {PageSize}) → {ex.GetType().Name}: {ex.Message}");
        }
    }

    // 4. Інші асинхронні операції CRUD та IEnumerable<T>
    private static async Task DemonstrateCrudOperationsAsync(CrudServiceAsync<Employee> employeeService)
    {
        PrintSection("4. Асинхронні операції CRUD та перебір сервісу як IEnumerable<T>");

        // сервіс реалізує IEnumerable<T>, тому з ним напряму працюють foreach і LINQ
        Programmer programmer = employeeService.OfType<Programmer>().First(p => p.Grade != ProgrammerGrade.Lead);
        Employee found = await employeeService.ReadAsync(programmer.Id);
        Console.WriteLine($"ReadAsync(Id)               → {found.ToShortString()}");

        programmer.Promote();
        Console.WriteLine($"UpdateAsync (підвищення)    → {await employeeService.UpdateAsync(programmer)}; новий грейд: {(await employeeService.ReadAsync(programmer.Id)).Position}");
        Console.WriteLine($"UpdateAsync (нема в сервісі)→ {await employeeService.UpdateAsync(Artist.CreateNew())}");
        Console.WriteLine($"CreateAsync (дублікат Id)   → {await employeeService.CreateAsync(programmer)}");

        Employee toRemove = employeeService.Last();
        Console.WriteLine($"RemoveAsync                 → {await employeeService.RemoveAsync(toRemove)}; повторно → {await employeeService.RemoveAsync(toRemove)}");

        try
        {
            await employeeService.ReadAsync(Guid.Empty);
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"ReadAsync(Guid.Empty)       → {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine($"LINQ по сервісу: програмістів — {employeeService.OfType<Programmer>().Count()}, " +
                          $"художників — {employeeService.OfType<Artist>().Count()}, усього — {employeeService.Count}");
        Console.WriteLine("foreach по сервісу (перші 3):");
        foreach (Employee employee in employeeService.Take(3))
        {
            Console.WriteLine($"  - {employee.ToShortString()}");
        }
    }

    // 5. Одночасні читання, записи і видалення з різних потоків
    private static async Task DemonstrateConcurrentAccessAsync(CrudServiceAsync<Employee> employeeService)
    {
        PrintSection("5. Потокобезпечність: одночасні Create, Remove та перебір з різних потоків (Task.Run + Task.WhenAll)");

        int countBefore = employeeService.Count;
        int created = 0, removed = 0, enumerations = 0;
        Employee[] removable = employeeService.Take(100).ToArray();

        // три задачі виконуються одночасно в потоках пулу і працюють з одним сервісом
        Task writer = Task.Run(async () =>
        {
            for (int i = 0; i < 300; i++)
            {
                if (await employeeService.CreateAsync(Programmer.CreateNew()))
                {
                    Interlocked.Increment(ref created);
                }
            }
        });

        Task remover = Task.Run(async () =>
        {
            foreach (Employee employee in removable)
            {
                if (await employeeService.RemoveAsync(employee))
                {
                    Interlocked.Increment(ref removed);
                }
            }
        });

        Task reader = Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                // перебір іде по знімку колекції, тому одночасні зміни не викликають винятків
                _ = employeeService.Sum(e => e.BaseSalary);
                Interlocked.Increment(ref enumerations);
            }
        });

        await Task.WhenAll(writer, remover, reader);

        int expected = countBefore + created - removed;
        Console.WriteLine($"Було: {countBefore}; створено: {created}; видалено: {removed}; повних переборів під час змін: {enumerations}");
        Console.WriteLine($"Стало: {employeeService.Count} (очікувалось {expected}) — " +
                          $"{(employeeService.Count == expected ? "дані узгоджені, винятків немає" : "ПОМИЛКА")}");
    }

    // 6. Збереження у файл і завантаження
    private static async Task SaveAndLoadAsync(
        CrudServiceAsync<Employee> employeeService,
        CrudServiceAsync<GameProject> projectService,
        CrudServiceAsync<BugReport> bugService)
    {
        PrintSection("6. Асинхронне збереження згенерованих колекцій у файли (SaveAsync) і завантаження (LoadAsync)");

        bool[] saved = await Task.WhenAll(employeeService.SaveAsync(), projectService.SaveAsync(), bugService.SaveAsync());
        Console.WriteLine($"SaveAsync трьох сервісів одночасно → {string.Join(", ", saved)}");
        foreach (string path in new[] { employeeService.FilePath, projectService.FilePath, bugService.FilePath })
        {
            Console.WriteLine($"  {Path.GetRelativePath(AppContext.BaseDirectory, path)} — {new FileInfo(path).Length:N0} байт");
        }

        // 10 одночасних SaveAsync в один файл: SemaphoreSlim пропускає їх по одному
        bool[] concurrentSaves = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => employeeService.SaveAsync()));
        Console.WriteLine($"10 одночасних SaveAsync в один файл → успішних: {concurrentSaves.Count(ok => ok)} з 10");

        var loadedEmployees = new CrudServiceAsync<Employee>(employeeService.FilePath);
        var loadedProjects = new CrudServiceAsync<GameProject>(projectService.FilePath);
        var loadedBugs = new CrudServiceAsync<BugReport>(bugService.FilePath);
        bool[] loaded = await Task.WhenAll(loadedEmployees.LoadAsync(), loadedProjects.LoadAsync(), loadedBugs.LoadAsync());

        Console.WriteLine($"LoadAsync у нові сервіси → {string.Join(", ", loaded)}; " +
                          $"працівників: {loadedEmployees.Count}, проєктів: {loadedProjects.Count}, багів: {loadedBugs.Count}");
        bool sameOrder = loadedEmployees.Select(e => e.Id).SequenceEqual(employeeService.Select(e => e.Id));
        bool sameTypes = loadedEmployees.Zip(employeeService).All(pair => pair.First.GetType() == pair.Second.GetType());
        Console.WriteLine($"Порядок елементів збережено: {sameOrder}; типи Programmer/Artist збережено: {sameTypes}; " +
                          $"фонд оплати праці збігається: {loadedEmployees.TotalMonthlyPayroll() == employeeService.TotalMonthlyPayroll()}");
    }
}
