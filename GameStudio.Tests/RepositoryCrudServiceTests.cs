using GameStudio.Common.Models;
using GameStudio.Common.Services;
using GameStudio.Infrastructure;
using GameStudio.Infrastructure.Mapping;
using GameStudio.Infrastructure.Models;
using GameStudio.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GameStudio.Tests;

/// <summary>
/// ЛР3: тести CRUD-сервісу, що працює через репозиторій з EF Core.
/// Кожен тест отримує окремий тимчасовий файл SQLite, схема створюється справжньою міграцією.
/// </summary>
[TestFixture]
public class RepositoryCrudServiceTests
{
    private string _databasePath = null!;

    [SetUp]
    public async Task SetUp()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"gamestudio-tests-{Guid.NewGuid():N}.db");
        await using GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath);
        await context.Database.MigrateAsync();
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }

    [Test]
    public async Task CreateAsync_ThenSaveAsync_PersistsEmployeesWithTheirTypes()
    {
        Programmer programmer = Programmer.CreateNew();
        Artist artist = Artist.CreateNew();

        await using (GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath))
        {
            CrudServiceAsync<Employee, EmployeeModel> service = EmployeeService(context);
            Assert.That(await service.CreateAsync(programmer), Is.True);
            Assert.That(await service.CreateAsync(artist), Is.True);
            Assert.That(await service.SaveAsync(), Is.True);
        }

        await using GameStudioContext readContext = GameStudioContext.CreateSqlite(_databasePath);
        CrudServiceAsync<Employee, EmployeeModel> reader = EmployeeService(readContext);
        Employee readProgrammer = await reader.ReadAsync(programmer.Id);
        Employee readArtist = await reader.ReadAsync(artist.Id);

        Assert.That(readProgrammer, Is.TypeOf<Programmer>());
        Assert.That(readArtist, Is.TypeOf<Artist>());
        Assert.That(readProgrammer.FullName, Is.EqualTo(programmer.FullName));
        Assert.That(((Programmer)readProgrammer).CommitsCount, Is.EqualTo(programmer.CommitsCount));
        Assert.That(await readContext.Programmers.CountAsync(), Is.EqualTo(1));
        Assert.That(await readContext.Artists.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateAsync_DuplicateId_ReturnsFalse()
    {
        await using GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath);
        CrudServiceAsync<Employee, EmployeeModel> service = EmployeeService(context);
        Programmer programmer = Programmer.CreateNew();

        Assert.That(await service.CreateAsync(programmer), Is.True);
        Assert.That(await service.CreateAsync(programmer), Is.False, "ще не збережений дублікат");
        await service.SaveAsync();
        Assert.That(await service.CreateAsync(programmer), Is.False, "збережений дублікат");
    }

    [Test]
    public async Task UpdateAsync_ThenSaveAsync_PersistsChanges()
    {
        Programmer programmer = Programmer.CreateNew();
        programmer.Grade = ProgrammerGrade.Junior;
        await SaveEmployeesAsync(programmer);

        await using (GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath))
        {
            CrudServiceAsync<Employee, EmployeeModel> service = EmployeeService(context);
            var stored = (Programmer)await service.ReadAsync(programmer.Id);
            stored.Promote();
            Assert.That(await service.UpdateAsync(stored), Is.True);
            Assert.That(await service.SaveAsync(), Is.True);
        }

        await using GameStudioContext readContext = GameStudioContext.CreateSqlite(_databasePath);
        var reloaded = (Programmer)await EmployeeService(readContext).ReadAsync(programmer.Id);
        Assert.That(reloaded.Grade, Is.EqualTo(ProgrammerGrade.Middle));
    }

    [Test]
    public async Task UpdateAsync_MissingElement_ReturnsFalse()
    {
        await using GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath);
        Assert.That(await EmployeeService(context).UpdateAsync(Artist.CreateNew()), Is.False);
    }

    [Test]
    public async Task ReadAsync_MissingId_ThrowsKeyNotFoundException()
    {
        await using GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath);
        CrudServiceAsync<Employee, EmployeeModel> service = EmployeeService(context);

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await service.ReadAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task ReadAllAsync_Paging_ReturnsRequestedPageInInsertionOrder()
    {
        Employee[] employees = Enumerable.Range(0, 7).Select(_ => (Employee)Programmer.CreateNew()).ToArray();
        foreach (Employee employee in employees)
        {
            await SaveEmployeesAsync(employee);
        }

        await using GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath);
        CrudServiceAsync<Employee, EmployeeModel> service = EmployeeService(context);

        Assert.That((await service.ReadAllAsync(2, 3)).Select(e => e.Id), Is.EqualTo(employees.Skip(3).Take(3).Select(e => e.Id)));
        Assert.That(await service.ReadAllAsync(4, 3), Is.Empty);
        Assert.That(await service.GetTotalPagesAsync(3), Is.EqualTo(3));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await service.ReadAllAsync(0, 3));
    }

    [Test]
    public async Task GameProject_RoundTrip_PreservesStorePageAndTeam()
    {
        Programmer programmer = Programmer.CreateNew();
        Artist artist = Artist.CreateNew();
        await SaveEmployeesAsync(programmer, artist);

        var project = new GameProject("Test Quest", "Puzzle", "Godot", 100_000m);
        project.Spend(25_000m);
        project.ChangeStatus(ProjectStatus.Production);
        project.StorePage = new StorePage("https://store.test/quest", 199m, new DateTime(2027, 1, 15)) { WishlistCount = 42 };
        project.AddTeamMember(programmer);
        project.AddTeamMember(artist);
        await SaveProjectAsync(project);

        await using GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath);
        GameProject read = await ProjectService(context).ReadAsync(project.Id);

        Assert.That(read.Title, Is.EqualTo("Test Quest"));
        Assert.That(read.Spent, Is.EqualTo(25_000m));
        Assert.That(read.Status, Is.EqualTo(ProjectStatus.Production));
        Assert.That(read.StorePage?.Url, Is.EqualTo("https://store.test/quest"));
        Assert.That(read.StorePage?.WishlistCount, Is.EqualTo(42));
        Assert.That(read.TeamMemberIds, Is.EquivalentTo(new[] { programmer.Id, artist.Id }));
        Assert.That(await context.ProjectAssignments.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task RemoveAsync_Project_DeletesRelatedRowsByCascade()
    {
        Programmer programmer = Programmer.CreateNew();
        await SaveEmployeesAsync(programmer);
        var project = new GameProject("Doomed", "Racing", "Unity", 50_000m) { StorePage = new StorePage("https://store.test/doomed", 0m, null) };
        project.AddTeamMember(programmer);
        await SaveProjectAsync(project);

        await using (GameStudioContext bugContext = GameStudioContext.CreateSqlite(_databasePath))
        {
            CrudServiceAsync<BugReport, BugReportModel> bugs = BugService(bugContext);
            await bugs.CreateAsync(new BugReport("Crash", BugSeverity.Critical, project.Id) { AssigneeId = programmer.Id });
            Assert.That(await bugs.SaveAsync(), Is.True);
        }

        await using (GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath))
        {
            CrudServiceAsync<GameProject, GameProjectModel> service = ProjectService(context);
            GameProject stored = await service.ReadAsync(project.Id);
            Assert.That(await service.RemoveAsync(stored), Is.True);
            Assert.That(await service.SaveAsync(), Is.True);
        }

        await using GameStudioContext check = GameStudioContext.CreateSqlite(_databasePath);
        Assert.That(await check.GameProjects.CountAsync(), Is.Zero);
        Assert.That(await check.StorePages.CountAsync(), Is.Zero);
        Assert.That(await check.ProjectAssignments.CountAsync(), Is.Zero);
        Assert.That(await check.BugReports.CountAsync(), Is.Zero);
        Assert.That(await check.Employees.CountAsync(), Is.EqualTo(1), "працівник не видаляється разом із проєктом");
    }

    [Test]
    public async Task BugReport_WithUnknownProject_ThrowsInvalidOperationException()
    {
        await using GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath);
        CrudServiceAsync<BugReport, BugReportModel> service = BugService(context);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CreateAsync(BugReport.CreateNew(Guid.NewGuid())));
    }

    [Test]
    public async Task CreateAsync_FromManyTasksConcurrently_IsSerializedSafely()
    {
        await using (GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath))
        {
            CrudServiceAsync<Employee, EmployeeModel> service = EmployeeService(context);
            Employee[] employees = Enumerable.Range(0, 50).Select(_ => (Employee)Artist.CreateNew()).ToArray();

            bool[] results = await Task.WhenAll(employees.Select(e => Task.Run(() => service.CreateAsync(e))));

            Assert.That(results, Is.All.True);
            Assert.That(await service.SaveAsync(), Is.True);
        }

        await using GameStudioContext check = GameStudioContext.CreateSqlite(_databasePath);
        Assert.That(await check.Artists.CountAsync(), Is.EqualTo(50));
    }

    private static CrudServiceAsync<Employee, EmployeeModel> EmployeeService(GameStudioContext context) =>
        new(new Repository<EmployeeModel>(context), new EmployeeMapper());

    private static CrudServiceAsync<GameProject, GameProjectModel> ProjectService(GameStudioContext context) =>
        new(new Repository<GameProjectModel>(context), new GameProjectMapper(context));

    private static CrudServiceAsync<BugReport, BugReportModel> BugService(GameStudioContext context) =>
        new(new Repository<BugReportModel>(context), new BugReportMapper(context));

    private async Task SaveEmployeesAsync(params Employee[] employees)
    {
        await using GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath);
        CrudServiceAsync<Employee, EmployeeModel> service = EmployeeService(context);
        foreach (Employee employee in employees)
        {
            await service.CreateAsync(employee);
        }

        Assert.That(await service.SaveAsync(), Is.True, service.LastSaveError);
    }

    private async Task SaveProjectAsync(GameProject project)
    {
        await using GameStudioContext context = GameStudioContext.CreateSqlite(_databasePath);
        CrudServiceAsync<GameProject, GameProjectModel> service = ProjectService(context);
        await service.CreateAsync(project);
        Assert.That(await service.SaveAsync(), Is.True, service.LastSaveError);
    }
}
