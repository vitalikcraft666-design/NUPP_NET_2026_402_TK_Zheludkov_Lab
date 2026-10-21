using GameStudio.Common.Extensions;
using GameStudio.Common.Models;
using GameStudio.Common.Services;
using static GameStudio.ConsoleApp.ConsoleUi;

namespace GameStudio.ConsoleApp.Demos;

/// <summary>
/// Демонстрація лабораторної роботи №1 (синхронний CrudService, події, методи розширення, Save/Load).
/// Запуск: dotnet run --project GameStudio.Console -- lab1
/// </summary>
internal static class Lab1Demo
{
    public static void Run()
    {
        Console.WriteLine("Лабораторна робота №1 — модель «Ігрова студія» (GameStudio)");
        Console.WriteLine($"Запуск: {DateTime.Now:dd.MM.yyyy HH:mm}");

        // ---------------------------------------------------------------------------------------------
        PrintSection("1. Статичні поля, статичні конструктори та статичні методи");

        // статичний метод викликається через ім'я класу, а не через об'єкт
        Console.WriteLine($"Працівників створено до початку роботи: {Employee.GetCreatedCount()}");
        Console.WriteLine($"Надбавка за рік стажу (статичне поле): {Employee.ExperienceBonusPerYear:P0}");
        Console.WriteLine($"Рушії, які підтримує студія: {string.Join(", ", GameProject.GetSupportedEngines())}");
        Console.WriteLine($"GameProject.IsEngineSupported(\"Godot\")     = {GameProject.IsEngineSupported("Godot")}");
        Console.WriteLine($"GameProject.IsEngineSupported(\"GameMaker\") = {GameProject.IsEngineSupported("GameMaker")}");

        // ---------------------------------------------------------------------------------------------
        PrintSection("2. CRUD-сервіс працівників: Create та ReadAll");

        var employeeService = new CrudService<Employee>();

        // підписка на події сервісу; обробники — лямбда-вирази
        employeeService.ElementCreated += employee => Console.WriteLine($"  [подія ElementCreated] додано: {employee.ToShortString()}");
        employeeService.ElementUpdated += employee => Console.WriteLine($"  [подія ElementUpdated] оновлено: {employee.ToShortString()}");
        employeeService.ElementRemoved += employee => Console.WriteLine($"  [подія ElementRemoved] видалено: {employee.ToShortString()}");

        var andrii = new Programmer("Андрій Коваленко", 30_000m, "C#", ProgrammerGrade.Middle, new DateTime(2021, 3, 15));
        var olena = new Programmer("Олена Шевчук", 28_000m, "Luau", ProgrammerGrade.Junior, new DateTime(2024, 9, 1));
        var maksym = new Artist("Максим Бондар", 26_000m, ArtSpecialization.Modeling3D, "Blender", new DateTime(2022, 6, 20));
        var iryna = new Artist("Ірина Мельник", 24_000m, ArtSpecialization.UserInterface, "Figma", new DateTime(2023, 2, 1));

        employeeService.Create(andrii);
        employeeService.Create(olena);
        employeeService.Create(maksym);
        employeeService.Create(iryna);

        Console.WriteLine();
        PrintAll("Усі працівники (ReadAll):", employeeService.ReadAll());
        Console.WriteLine($"Кількість елементів у сервісі: {employeeService.Count}");
        Console.WriteLine($"Працівників створено (статичний лічильник): {Employee.GetCreatedCount()}");

        // ---------------------------------------------------------------------------------------------
        PrintSection("3. Read — пошук працівника за Id");

        Employee found = employeeService.Read(olena.Id);
        Console.WriteLine($"Read({olena.Id}):");
        Console.WriteLine($"  {found}");

        // ---------------------------------------------------------------------------------------------
        PrintSection("4. Update — зміна даних працівників");

        // варіант 1: змінюємо об'єкт через його методи і повідомляємо сервіс про оновлення
        olena.MakeCommits(42);
        bool promoted = olena.Promote();
        Console.WriteLine($"Комітів у Олени: {olena.CommitsCount}; підвищення грейду: {(promoted ? "так" : "ні")} → {olena.Grade}");
        employeeService.Update(olena);

        maksym.CompleteArtwork();
        maksym.CompleteArtwork();
        maksym.CompleteArtwork();
        Console.WriteLine($"Завершених 3D-моделей у Максима: {maksym.ArtworksCompleted} (бонус {Artist.BonusPerArtwork:N0} грн за кожну)");
        employeeService.Update(maksym);

        // варіант 2: замінюємо елемент новим об'єктом з тим самим Id
        var updatedIryna = new Artist("Ірина Мельник", 27_500m, ArtSpecialization.Animation, "Spine", iryna.HireDate)
        {
            Id = iryna.Id,
            ArtworksCompleted = 2
        };
        employeeService.Update(updatedIryna);

        Console.WriteLine();
        PrintAll("Працівники після оновлення:", employeeService.ReadAll());

        // ---------------------------------------------------------------------------------------------
        PrintSection("5. Remove — видалення працівника");

        employeeService.Remove(updatedIryna);
        Console.WriteLine();
        PrintAll("Працівники після видалення:", employeeService.ReadAll());

        // ---------------------------------------------------------------------------------------------
        PrintSection("6. Обробка винятків у CRUD-сервісі");

        try
        {
            employeeService.Read(Guid.Empty);
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"Read неіснуючого Id    → {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            employeeService.Create(andrii);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Повторний Create       → {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            employeeService.Remove(updatedIryna);
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"Повторний Remove       → {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            andrii.MakeCommits(-5);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"MakeCommits(-5)        → {ex.GetType().Name}: {ex.Message}");
        }

        // ---------------------------------------------------------------------------------------------
        PrintSection("7. Методи розширення та пошук за умовою (делегати Func<T, bool> і Predicate<T>)");

        IEnumerable<Employee> staff = employeeService.ReadAll();

        // виклик методів розширення — так, ніби це методи самої колекції / самого об'єкта
        Console.WriteLine($"Місячний фонд оплати праці (TotalMonthlyPayroll): {staff.TotalMonthlyPayroll():N2} грн");
        Console.WriteLine($"Короткий опис (ToShortString): {andrii.ToShortString()}");

        // умова пошуку передається у вигляді делегата Func<Employee, bool>
        Func<Employee, bool> isProgrammer = employee => employee is Programmer;
        PrintAll("Лише програмісти (Find з Func<Employee, bool>):", employeeService.Find(isProgrammer));

        // вбудований делегат Predicate<T> — метод, що повертає bool
        Predicate<Employee> earnsMoreThan40K = employee => employee.CalculateMonthlySalary() > 40_000m;
        Console.WriteLine("Зарплата понад 40 000 грн (Predicate<Employee>):");
        foreach (Employee employee in staff)
        {
            if (earnsMoreThan40K(employee))
            {
                Console.WriteLine($"  - {employee.ToShortString()}: {employee.CalculateMonthlySalary():N2} грн");
            }
        }

        // ---------------------------------------------------------------------------------------------
        PrintSection("8. Ігрові проєкти: власний делегат і події StatusChanged / BudgetExceeded");

        var projectService = new CrudService<GameProject>();
        projectService.ElementCreated += project => Console.WriteLine($"  [подія ElementCreated] додано проєкт «{project.Title}»");

        var skyPirates = new GameProject("Sky Pirates", "Action RPG", "Roblox Studio", 500_000m);
        var pixelFarm = new GameProject("Pixel Farm", "Simulator", "Godot", 150_000m);

        // підписка на подію методом, сигнатура якого відповідає делегату ProjectStatusChangedHandler
        skyPirates.StatusChanged += OnProjectStatusChanged;
        pixelFarm.StatusChanged += OnProjectStatusChanged;

        // підписка на подію лямбда-виразом (делегат Action<GameProject, decimal>)
        skyPirates.BudgetExceeded += (project, overrun) =>
            Console.WriteLine($"  [подія BudgetExceeded] «{project.Title}»: бюджет перевищено на {overrun:N2} грн!");

        skyPirates.AddTeamMember(andrii);
        skyPirates.AddTeamMember(olena);
        skyPirates.AddTeamMember(maksym);
        pixelFarm.AddTeamMember(andrii);

        projectService.Create(skyPirates);
        projectService.Create(pixelFarm);

        Console.WriteLine();
        skyPirates.ChangeStatus(ProjectStatus.Prototype);
        skyPirates.ChangeStatus(ProjectStatus.Production);
        pixelFarm.ChangeStatus(ProjectStatus.Prototype);

        skyPirates.Spend(320_000m);
        Console.WriteLine($"Витрачено 320 000 грн → використано бюджету: {skyPirates.BudgetUsagePercent()} %");
        skyPirates.Spend(250_000m);
        Console.WriteLine($"Витрачено ще 250 000 грн → використано бюджету: {skyPirates.BudgetUsagePercent()} %");

        skyPirates.ChangeStatus(ProjectStatus.Testing);
        skyPirates.ChangeStatus(ProjectStatus.Released);
        projectService.Update(skyPirates);

        try
        {
            skyPirates.ChangeStatus(ProjectStatus.Production);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Зміна статусу після релізу → {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            _ = new GameProject("Retro Racer", "Racing", "GameMaker", 80_000m);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Непідтримуваний рушій       → {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine();
        PrintAll("Усі проєкти (ReadAll):", projectService.ReadAll());

        // ---------------------------------------------------------------------------------------------
        PrintSection("9. Баг-трекер: подія Fixed (вбудований делегат EventHandler)");

        var bugService = new CrudService<BugReport>();

        var fallingBug = new BugReport("Гравець провалюється крізь палубу корабля", BugSeverity.Critical, skyPirates.Id);
        var saveBug = new BugReport("Не зберігається прогрес після виходу з гри", BugSeverity.High, skyPirates.Id);
        var typoBug = new BugReport("Друкарська помилка в головному меню", BugSeverity.Low, pixelFarm.Id);

        foreach (BugReport bug in new[] { fallingBug, saveBug, typoBug })
        {
            bug.Fixed += OnBugFixed;
            bugService.Create(bug);
        }

        PrintAll("Усі баги:", bugService.ReadAll());
        Console.WriteLine($"Є відкриті критичні баги (HasOpenCriticalBugs): {bugService.ReadAll().HasOpenCriticalBugs()}");

        Console.WriteLine();
        fallingBug.AssignTo(andrii);
        fallingBug.MarkAsFixed();
        typoBug.AssignTo(olena);
        typoBug.MarkAsFixed();

        try
        {
            saveBug.MarkAsFixed();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"MarkAsFixed без виконавця → {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine($"Є відкриті критичні баги (HasOpenCriticalBugs): {bugService.ReadAll().HasOpenCriticalBugs()}");
        PrintAll("Відкриті баги (Find з умовою !IsFixed):", bugService.Find(bug => !bug.IsFixed));

        // ---------------------------------------------------------------------------------------------
        PrintSection("10. Додаткове завдання: Save / Load — серіалізація у JSON-файли");

        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        string employeesFile = Path.Combine(dataDirectory, "employees.json");
        string projectsFile = Path.Combine(dataDirectory, "projects.json");
        string bugsFile = Path.Combine(dataDirectory, "bugs.json");

        employeeService.Save(employeesFile);
        projectService.Save(projectsFile);
        bugService.Save(bugsFile);

        foreach (string file in new[] { employeesFile, projectsFile, bugsFile })
        {
            Console.WriteLine($"Save → {Path.GetRelativePath(AppContext.BaseDirectory, file)} ({new FileInfo(file).Length} байт)");
        }

        Console.WriteLine();
        Console.WriteLine("Фрагмент файлу employees.json:");
        foreach (string line in File.ReadLines(employeesFile).Take(12))
        {
            Console.WriteLine($"  {line}");
        }
        Console.WriteLine("  ...");

        // нові порожні сервіси, які отримують дані лише з файлів
        var loadedEmployees = new CrudService<Employee>();
        var loadedProjects = new CrudService<GameProject>();
        var loadedBugs = new CrudService<BugReport>();

        loadedEmployees.Load(employeesFile);
        loadedProjects.Load(projectsFile);
        loadedBugs.Load(bugsFile);

        Console.WriteLine();
        Console.WriteLine($"Load → працівників: {loadedEmployees.Count}, проєктів: {loadedProjects.Count}, багів: {loadedBugs.Count}");
        Console.WriteLine("Завантажені працівники (тип об'єкта збережено завдяки полю \"$type\"):");
        foreach (Employee employee in loadedEmployees.ReadAll())
        {
            Console.WriteLine($"  {employee.GetType().Name,-10} | {employee}");
        }

        PrintAll("Завантажені проєкти:", loadedProjects.ReadAll());
        PrintAll("Завантажені баги:", loadedBugs.ReadAll());

        bool sameEmployees = loadedEmployees.ReadAll().Select(e => e.Id).Order()
            .SequenceEqual(employeeService.ReadAll().Select(e => e.Id).Order());
        bool sameSalaries = loadedEmployees.ReadAll().TotalMonthlyPayroll() == employeeService.ReadAll().TotalMonthlyPayroll();
        Console.WriteLine($"Ідентифікатори збігаються з оригіналом: {sameEmployees}; фонд оплати праці збігається: {sameSalaries}");

        try
        {
            loadedEmployees.Load(Path.Combine(dataDirectory, "missing.json"));
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Load неіснуючого файлу → {ex.GetType().Name}: {ex.Message}");
        }

        // ---------------------------------------------------------------------------------------------
        PrintSection("Підсумок");
        Console.WriteLine($"Усього створено об'єктів Employee (разом із завантаженими з файлу): {Employee.GetCreatedCount()}");
        Console.WriteLine("Роботу програми завершено.");
    }

    // метод-обробник події StatusChanged: сигнатура збігається з делегатом ProjectStatusChangedHandler
    private static void OnProjectStatusChanged(GameProject project, ProjectStatus oldStatus, ProjectStatus newStatus)
    {
        Console.WriteLine($"  [подія StatusChanged] «{project.Title}»: {oldStatus} → {newStatus}");
    }

    // метод-обробник події Fixed: сигнатура збігається з вбудованим делегатом EventHandler
    private static void OnBugFixed(object? sender, EventArgs e)
    {
        if (sender is BugReport bug)
        {
            Console.WriteLine($"  [подія Fixed] виправлено баг: {bug.Title}");
        }
    }
}
