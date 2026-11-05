using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GameStudio.Common.Models;
using GameStudio.Common.Security;
using GameStudio.REST.Models;

namespace GameStudio.Tests;

/// <summary>
/// ЛР5: реєстрація, вхід, токени Bearer і розподіл доступу за ролями (Tester, Developer, Manager, Admin).
/// Для кожної ролі перевіряється, що дозволені запити виконуються, а заборонені повертають 401 або 403.
/// </summary>
[TestFixture]
public class AuthTests : ApiTestBase
{
    private static ProjectCreateModel NewProject(string title) =>
        new() { Title = title, Genre = "Adventure", Engine = "Godot", Budget = 100_000m };

    [Test]
    public async Task Register_ThenLogin_ReturnsBearerAndRefreshTokens()
    {
        Assert.That((await RegisterAsync("player@test.ua")).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        TokenResponse token = await GetTokenAsync("player@test.ua");

        Assert.Multiple(() =>
        {
            Assert.That(token.TokenType, Is.EqualTo("Bearer"));
            Assert.That(token.AccessToken, Is.Not.Empty);
            Assert.That(token.RefreshToken, Is.Not.Empty);
            Assert.That(token.ExpiresIn, Is.EqualTo(3600));
        });
    }

    [Test]
    public async Task Register_RejectsWeakPasswordAndDuplicateEmail()
    {
        Assert.That((await RegisterAsync("weak@test.ua", "12345")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await RegisterAsync("same@test.ua")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await RegisterAsync("same@test.ua")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await RegisterAsync("user@test.ua");

        HttpResponseMessage response = await Anonymous.PostAsJsonAsync("/api/auth/login",
            new { email = "user@test.ua", password = "Wrong#Password1" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task NewUser_GetsTesterRole_AndCanUpdateProfile()
    {
        HttpClient tester = await CreateUserAsync("tester@test.ua");

        AccountModel me = (await tester.GetFromJsonAsync<AccountModel>("/api/account/me", Json))!;
        Assert.That(me.Roles, Is.EqualTo(new[] { StudioRoles.Tester }));

        HttpResponseMessage update = await tester.PutAsJsonAsync("/api/account/me", new AccountUpdateModel { FullName = "Олена Тестувальниця" }, Json);
        Assert.That(update.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Read<AccountModel>(update)).FullName, Is.EqualTo("Олена Тестувальниця"));
    }

    [Test]
    public async Task Anonymous_CanReadPublicData_ButGets401ForProtectedEndpoints()
    {
        Assert.That((await Anonymous.GetAsync("/api/projects")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Anonymous.GetAsync("/api/bugs")).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        HttpResponseMessage post = await Anonymous.PostAsJsonAsync("/api/projects", NewProject("Без токена"), Json);
        Assert.That(post.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(post.Headers.WwwAuthenticate.ToString(), Does.Contain("Bearer"));
        Assert.That(post.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"));

        Assert.That((await Anonymous.GetAsync("/api/programmers")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await Anonymous.GetAsync("/api/account/me")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task InvalidToken_Returns401()
    {
        using HttpClient client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        Assert.That((await client.GetAsync("/api/account/me")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Tester_CanReportBugs_ButGets403ForManagement()
    {
        ProjectModel project = await Read<ProjectModel>(await Admin.PostAsJsonAsync("/api/projects", NewProject("Для тестувальника"), Json));
        HttpClient tester = await CreateUserAsync("tester@test.ua");

        HttpResponseMessage bug = await tester.PostAsJsonAsync("/api/bugs",
            new BugCreateModel { Title = "Падає гра на старті", Severity = BugSeverity.Critical, ProjectId = project.Id }, Json);
        Assert.That(bug.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        BugModel created = await Read<BugModel>(bug);
        Assert.Multiple(async () =>
        {
            Assert.That((await tester.PostAsJsonAsync("/api/projects", NewProject("Не можна"), Json)).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That((await tester.PutAsJsonAsync($"/api/bugs/{created.Id}", new BugUpdateModel { Title = "Змінено", Severity = BugSeverity.Low }, Json)).StatusCode,
                Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That((await tester.GetAsync("/api/programmers")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That((await tester.GetAsync("/api/users")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        });
    }

    [Test]
    public async Task Developer_CanUpdateBugs_ButNotProjects()
    {
        ProjectModel project = await Read<ProjectModel>(await Admin.PostAsJsonAsync("/api/projects", NewProject("Для розробника"), Json));
        BugModel bug = await Read<BugModel>(await Admin.PostAsJsonAsync("/api/bugs",
            new BugCreateModel { Title = "Не працює збереження", Severity = BugSeverity.High, ProjectId = project.Id }, Json));
        HttpClient developer = await CreateUserAsync("dev@test.ua", StudioRoles.Developer);

        HttpResponseMessage update = await developer.PutAsJsonAsync($"/api/bugs/{bug.Id}",
            new BugUpdateModel { Title = bug.Title, Severity = BugSeverity.Medium }, Json);

        Assert.That(update.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await developer.GetAsync("/api/programmers")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await developer.DeleteAsync($"/api/bugs/{bug.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await developer.PutAsJsonAsync($"/api/projects/{project.Id}",
            new ProjectUpdateModel { Title = "Ні", Genre = "Ні", Engine = "Godot", Budget = 1, Status = ProjectStatus.Concept }, Json)).StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Manager_ManagesProjects_ButOnlyAdminDeletesThemAndHiresEmployees()
    {
        HttpClient manager = await CreateUserAsync("producer@test.ua", StudioRoles.Manager);

        HttpResponseMessage created = await manager.PostAsJsonAsync("/api/projects", NewProject("Проєкт продюсера"), Json);
        Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        ProjectModel project = await Read<ProjectModel>(created);

        Assert.That((await manager.DeleteAsync($"/api/projects/{project.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await manager.PostAsJsonAsync("/api/programmers", new ProgrammerRequestModel
        {
            FullName = "Новий Програміст", BaseSalary = 30_000m, PrimaryLanguage = "C#", Grade = ProgrammerGrade.Junior
        }, Json)).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        Assert.That((await Admin.DeleteAsync($"/api/projects/{project.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Admin_ManagesUserRoles()
    {
        await RegisterAsync("worker@test.ua");
        string id = await FindUserIdAsync("worker@test.ua");

        HttpResponseMessage grant = await Admin.PutAsync($"/api/users/{id}/roles/{StudioRoles.Manager}", null);
        Assert.That(grant.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Read<AccountModel>(grant)).Roles, Is.EquivalentTo(new[] { StudioRoles.Manager, StudioRoles.Tester }));

        Assert.That((await Admin.PutAsync($"/api/users/{id}/roles/Superhero", null)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        HttpResponseMessage revoke = await Admin.DeleteAsync($"/api/users/{id}/roles/{StudioRoles.Manager}");
        Assert.That((await Read<AccountModel>(revoke)).Roles, Is.EqualTo(new[] { StudioRoles.Tester }));

        string adminId = await FindUserIdAsync(AdminEmail);
        Assert.That((await Admin.DeleteAsync($"/api/users/{adminId}/roles/{StudioRoles.Admin}")).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task RefreshToken_ReturnsNewAccessTokenWithCurrentRoles()
    {
        await RegisterAsync("grow@test.ua");
        TokenResponse first = await GetTokenAsync("grow@test.ua");
        await Admin.PutAsync($"/api/users/{await FindUserIdAsync("grow@test.ua")}/roles/{StudioRoles.Manager}", null);

        HttpResponseMessage refresh = await Anonymous.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        Assert.That(refresh.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        TokenResponse second = await Read<TokenResponse>(refresh);

        using HttpClient client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second.AccessToken);
        Assert.That((await client.PostAsJsonAsync("/api/projects", NewProject("Після оновлення токена"), Json)).StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
    }
}
