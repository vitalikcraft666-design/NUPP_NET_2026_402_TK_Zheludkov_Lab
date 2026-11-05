using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameStudio.REST.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace GameStudio.Tests;

/// <summary>
/// Спільна основа інтеграційних тестів REST API: WebApplicationFactory запускає GameStudio.REST у пам'яті
/// з окремим тимчасовим файлом SQLite. ЛР5: під час запуску застосунок створює ролі й адміністратора,
/// а допоміжні методи реєструють користувачів, отримують токени Bearer і надають ролі.
/// </summary>
public abstract class ApiTestBase
{
    protected const string AdminEmail = "admin@test.gamestudio.ua";
    protected const string AdminPassword = "Admin#Test2026";
    protected const string UserPassword = "User#Test2026";

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private string _databasePath = null!;

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

    /// <summary>Клієнт без токена (анонімний користувач).</summary>
    protected HttpClient Anonymous { get; private set; } = null!;

    /// <summary>Клієнт з токеном адміністратора.</summary>
    protected HttpClient Admin { get; private set; } = null!;

    protected string DatabasePath => _databasePath;

    [SetUp]
    public async Task SetUpApiAsync()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"gamestudio-api-{Guid.NewGuid():N}.db");
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GameStudio", $"Data Source={_databasePath}");
            builder.UseSetting("Seed:AdminEmail", AdminEmail);
            builder.UseSetting("Seed:AdminPassword", AdminPassword);
        });

        Anonymous = Factory.CreateClient();
        Admin = await LoginAsync(AdminEmail, AdminPassword);
    }

    [TearDown]
    public void TearDownApi()
    {
        Anonymous.Dispose();
        Admin.Dispose();
        Factory.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }

    // POST /api/auth/register — вбудована кінцева точка Identity
    protected async Task<HttpResponseMessage> RegisterAsync(string email, string password = UserPassword) =>
        await Anonymous.PostAsJsonAsync("/api/auth/register", new { email, password });

    // POST /api/auth/login → токен доступу; повертає клієнт із заголовком Authorization: Bearer <токен>
    protected async Task<HttpClient> LoginAsync(string email, string password = UserPassword)
    {
        TokenResponse token = await GetTokenAsync(email, password);
        HttpClient client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    protected async Task<TokenResponse> GetTokenAsync(string email, string password = UserPassword)
    {
        HttpResponseMessage response = await Anonymous.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"вхід {email}");
        return (await response.Content.ReadFromJsonAsync<TokenResponse>(Json))!;
    }

    // реєстрація + (за потреби) роль від адміністратора + вхід; роль потрапляє в новий токен
    protected async Task<HttpClient> CreateUserAsync(string email, string? role = null)
    {
        Assert.That((await RegisterAsync(email)).StatusCode, Is.EqualTo(HttpStatusCode.OK), $"реєстрація {email}");
        if (role is not null)
        {
            string userId = await FindUserIdAsync(email);
            HttpResponseMessage grant = await Admin.PutAsync($"/api/users/{userId}/roles/{role}", null);
            Assert.That(grant.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"роль {role} для {email}");
        }

        return await LoginAsync(email);
    }

    protected async Task<string> FindUserIdAsync(string email)
    {
        List<AccountModel> users = await Admin.GetFromJsonAsync<List<AccountModel>>("/api/users", Json) ?? new();
        return users.Single(u => u.Email == email).Id;
    }

    protected static async Task<T> Read<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    protected sealed record TokenResponse(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);
}
