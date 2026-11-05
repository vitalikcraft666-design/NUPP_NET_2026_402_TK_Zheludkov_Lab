using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStudio.Common.Models;
using GameStudio.REST.Models;

namespace GameStudio.Tests;

/// <summary>
/// ЛР4: інтеграційні тести REST API. WebApplicationFactory запускає застосунок GameStudio.REST у пам'яті
/// (без мережі) з окремим тимчасовим файлом SQLite, тож тести не змінюють Database/gamestudio.db.
/// Перевіряються коди відповідей HTTP і вміст JSON для всіх контролерів.
/// ЛР5: запити на зміну даних виконуються з токеном адміністратора (клієнт _client), права ролей перевіряє AuthTests.
/// </summary>
[TestFixture]
public class RestApiTests : ApiTestBase
{
    private HttpClient _client = null!;
    private string _databasePath = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Admin;
        _databasePath = DatabasePath;
    }

    [Test]
    public async Task Api_UsesTemporaryDatabase()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/projects");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(File.Exists(_databasePath), Is.True, "застосунок має працювати з тимчасовою БД тесту");
        Assert.That(await Read<List<ProjectSummaryModel>>(response), Is.Empty);
    }

    [Test]
    public async Task CreateProject_Returns201WithLocation_AndCanBeReadBack()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/projects", NewProject("Кобзар-квест"), Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        ProjectModel created = await Read<ProjectModel>(response);
        Assert.Multiple(() =>
        {
            Assert.That(response.Headers.Location!.AbsolutePath, Is.EqualTo($"/api/projects/{created.Id}"));
            Assert.That(created.Status, Is.EqualTo(ProjectStatus.Concept));
            Assert.That(created.Spent, Is.Zero);
        });

        HttpResponseMessage get = await _client.GetAsync(response.Headers.Location);
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Read<ProjectModel>(get)).Title, Is.EqualTo("Кобзар-квест"));
    }

    [Test]
    public async Task CreateProject_WithInvalidData_Returns400WithFieldErrors()
    {
        var invalid = new ProjectCreateModel { Title = "X", Genre = "Puzzle", Engine = "CryEngine", Budget = 0 };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/projects", invalid, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"));
        JsonElement errors = (await Read<JsonElement>(response)).GetProperty("errors");
        Assert.Multiple(() =>
        {
            Assert.That(errors.TryGetProperty("title", out _), Is.True);
            Assert.That(errors.TryGetProperty("engine", out _), Is.True);
            Assert.That(errors.TryGetProperty("budget", out _), Is.True);
        });
    }

    [Test]
    public async Task UnknownIds_Return404ForEveryResource()
    {
        Guid id = Guid.NewGuid();

        foreach (string url in new[] { $"/api/projects/{id}", $"/api/bugs/{id}", $"/api/programmers/{id}", $"/api/artists/{id}" })
        {
            HttpResponseMessage response = await _client.GetAsync(url);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), url);
        }

        HttpResponseMessage delete = await _client.DeleteAsync($"/api/projects/{id}");
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Pagination_ReturnsRequestedPage_AndRejectsInvalidParameters()
    {
        for (int i = 1; i <= 5; i++)
        {
            await CreateProjectAsync($"Проєкт {i}");
        }

        List<ProjectSummaryModel> page2 = await _client.GetFromJsonAsync<List<ProjectSummaryModel>>("/api/projects?page=2&amount=2", Json) ?? new();
        List<ProjectSummaryModel> page4 = await _client.GetFromJsonAsync<List<ProjectSummaryModel>>("/api/projects?page=4&amount=2", Json) ?? new();

        Assert.That(page2.Select(p => p.Title), Is.EqualTo(new[] { "Проєкт 3", "Проєкт 4" }));
        Assert.That(page4, Is.Empty);
        Assert.That((await _client.GetAsync("/api/projects?page=0")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await _client.GetAsync("/api/projects?amount=101")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UpdateProject_Returns200_AndFinishedProjectStatusChangeReturns409()
    {
        ProjectModel project = await CreateProjectAsync("Степовий вітер");
        ProjectUpdateModel update = new()
        {
            Title = "Степовий вітер 2", Genre = "Strategy", Engine = "Unity", Budget = 900_000m, Status = ProjectStatus.Released
        };

        HttpResponseMessage put = await _client.PutAsJsonAsync($"/api/projects/{project.Id}", update, Json);
        Assert.That(put.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        ProjectModel updated = await Read<ProjectModel>(put);
        Assert.That((updated.Title, updated.Status, updated.Budget), Is.EqualTo(("Степовий вітер 2", ProjectStatus.Released, 900_000m)));

        // назву завершеного проєкту змінити можна, а статус — ні
        update.Title = "Степовий вітер 2: Remastered";
        Assert.That((await _client.PutAsJsonAsync($"/api/projects/{project.Id}", update, Json)).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        update.Status = ProjectStatus.Testing;
        HttpResponseMessage conflict = await _client.PutAsJsonAsync($"/api/projects/{project.Id}", update, Json);
        Assert.That(conflict.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task DeleteProject_Returns204_AndCascadesToBugs()
    {
        ProjectModel project = await CreateProjectAsync("Тимчасовий");
        BugModel bug = await CreateBugAsync(project.Id, "Гра зависає на екрані завантаження");

        HttpResponseMessage delete = await _client.DeleteAsync($"/api/projects/{project.Id}");

        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await _client.GetAsync($"/api/projects/{project.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await _client.GetAsync($"/api/bugs/{bug.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await _client.DeleteAsync($"/api/projects/{project.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task StorePage_PutCreatesThenReplaces_DeleteRemoves()
    {
        ProjectModel project = await CreateProjectAsync("Карпатський рейд");
        string url = $"/api/projects/{project.Id}/store-page";
        var page = new StorePageModel { Url = "https://store.gamestudio.ua/games/raid", Price = 349m, WishlistCount = 10 };

        Assert.That((await _client.GetAsync(url)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await _client.PutAsJsonAsync(url, page, Json)).StatusCode, Is.EqualTo(HttpStatusCode.Created));

        page.Price = 299m;
        Assert.That((await _client.PutAsJsonAsync(url, page, Json)).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await _client.GetFromJsonAsync<StorePageModel>(url, Json))!.Price, Is.EqualTo(299m));

        Assert.That((await _client.DeleteAsync(url)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await _client.GetAsync(url)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Team_AddAndRemoveMembers()
    {
        ProjectModel project = await CreateProjectAsync("Козацька ліга");
        ProgrammerModel programmer = await CreateProgrammerAsync("Шевчук Андрій");
        HttpResponseMessage artistResponse = await _client.PostAsJsonAsync("/api/artists", new ArtistRequestModel
        {
            FullName = "Лисенко Ірина", BaseSalary = 30_000m, Specialization = ArtSpecialization.Concept, MainTool = "Krita"
        }, Json);
        ArtistModel artist = await Read<ArtistModel>(artistResponse);
        string team = $"/api/projects/{project.Id}/team";

        Assert.That((await _client.PutAsync($"{team}/{programmer.Id}", null)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await _client.PutAsync($"{team}/{artist.Id}", null)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await _client.PutAsync($"{team}/{artist.Id}", null)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent), "PUT ідемпотентний");
        Assert.That((await _client.PutAsync($"{team}/{Guid.NewGuid()}", null)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        List<TeamMemberModel> members = await _client.GetFromJsonAsync<List<TeamMemberModel>>(team, Json) ?? new();
        Assert.That(members.Select(m => m.Type), Is.EquivalentTo(new[] { "Programmer", "Artist" }));

        Assert.That((await _client.DeleteAsync($"{team}/{artist.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await _client.DeleteAsync($"{team}/{artist.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        members = await _client.GetFromJsonAsync<List<TeamMemberModel>>(team, Json) ?? new();
        Assert.That(members.Select(m => m.Id), Is.EqualTo(new[] { programmer.Id }));
    }

    [Test]
    public async Task Bugs_ValidateReferences_AndCanBeFixedOnlyWithAssignee()
    {
        ProjectModel project = await CreateProjectAsync("Чорноморський шторм");
        ProgrammerModel programmer = await CreateProgrammerAsync("Коваль Олег");

        var orphan = new BugCreateModel { Title = "Баг без проєкту", Severity = BugSeverity.Low, ProjectId = Guid.NewGuid() };
        HttpResponseMessage badProject = await _client.PostAsJsonAsync("/api/bugs", orphan, Json);
        Assert.That(badProject.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Read<JsonElement>(badProject)).GetProperty("errors").TryGetProperty("projectId", out _), Is.True);

        BugModel bug = await CreateBugAsync(project.Id, "Текстури мерехтять у дощ");
        var update = new BugUpdateModel { Title = bug.Title, Severity = BugSeverity.High, IsFixed = true };
        Assert.That((await _client.PutAsJsonAsync($"/api/bugs/{bug.Id}", update, Json)).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        update.AssigneeId = programmer.Id;
        HttpResponseMessage fixedResponse = await _client.PutAsJsonAsync($"/api/bugs/{bug.Id}", update, Json);
        Assert.That(fixedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        BugModel fixedBug = await Read<BugModel>(fixedResponse);
        Assert.That((fixedBug.IsFixed, fixedBug.Severity, fixedBug.AssigneeId), Is.EqualTo((true, BugSeverity.High, (Guid?)programmer.Id)));

        List<BugModel> projectBugs = await _client.GetFromJsonAsync<List<BugModel>>($"/api/projects/{project.Id}/bugs", Json) ?? new();
        Assert.That(projectBugs.Select(b => b.Id), Is.EqualTo(new[] { bug.Id }));
    }

    [Test]
    public async Task DeleteProgrammer_Returns204_AndLeavesBugsWithoutAssignee()
    {
        ProjectModel project = await CreateProjectAsync("Дніпровські легенди");
        ProgrammerModel programmer = await CreateProgrammerAsync("Бондар Максим");
        HttpResponseMessage created = await _client.PostAsJsonAsync("/api/bugs", new BugCreateModel
        {
            Title = "Не зберігається прогрес", Severity = BugSeverity.Critical, ProjectId = project.Id, AssigneeId = programmer.Id
        }, Json);
        BugModel bug = await Read<BugModel>(created);

        Assert.That((await _client.DeleteAsync($"/api/programmers/{programmer.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        BugModel after = (await _client.GetFromJsonAsync<BugModel>($"/api/bugs/{bug.Id}", Json))!;
        Assert.That(after.AssigneeId, Is.Null);
    }

    [Test]
    public async Task Programmer_UpdateRecalculatesComputedFields()
    {
        ProgrammerModel programmer = await CreateProgrammerAsync("Мельник Тарас");
        var request = new ProgrammerRequestModel
        {
            FullName = "Мельник Тарас", BaseSalary = 40_000m, PrimaryLanguage = "C++", Grade = ProgrammerGrade.Senior,
            HireDate = DateTime.Today.AddYears(-3), CommitsCount = 900
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/programmers/{programmer.Id}", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        ProgrammerModel updated = await Read<ProgrammerModel>(response);
        Assert.Multiple(() =>
        {
            Assert.That(updated.Position, Is.EqualTo("Senior C++-програміст"));
            Assert.That(updated.YearsOfExperience, Is.EqualTo(3));
            Assert.That(updated.MonthlySalary, Is.EqualTo(Math.Round(40_000m * 2.0m * 1.09m, 2)));
        });
    }

    // ---------- допоміжні методи ----------

    private static ProjectCreateModel NewProject(string title) =>
        new() { Title = title, Genre = "Adventure", Engine = "Godot", Budget = 420_000m };

    private async Task<ProjectModel> CreateProjectAsync(string title)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/projects", NewProject(title), Json);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return await Read<ProjectModel>(response);
    }

    private async Task<ProgrammerModel> CreateProgrammerAsync(string fullName)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/programmers", new ProgrammerRequestModel
        {
            FullName = fullName, BaseSalary = 35_000m, PrimaryLanguage = "C#", Grade = ProgrammerGrade.Middle
        }, Json);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return await Read<ProgrammerModel>(response);
    }

    private async Task<BugModel> CreateBugAsync(Guid projectId, string title)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/bugs",
            new BugCreateModel { Title = title, Severity = BugSeverity.Medium, ProjectId = projectId }, Json);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return await Read<BugModel>(response);
    }
}
