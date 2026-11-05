using System.Reflection;
using System.Text.Json.Serialization;
using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.Common.Services;
using GameStudio.Infrastructure;
using GameStudio.Infrastructure.Identity;
using GameStudio.Infrastructure.Mapping;
using GameStudio.Infrastructure.Repositories;
using GameStudio.REST.Swagger;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Db = GameStudio.Infrastructure.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Рівень доступу до даних (GameStudio.Infrastructure) ----------
// Рядок підключення береться з appsettings.json (ConnectionStrings:GameStudio) або змінної середовища
// ConnectionStrings__GameStudio; якщо він порожній — використовується файл Database/gamestudio.db з ЛР3.
string? connectionString = builder.Configuration.GetConnectionString("GameStudio");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = $"Data Source={DatabaseLocator.GetDefaultDatabasePath()}";
}

// контекст БД — Scoped: окремий екземпляр на кожен HTTP-запит
builder.Services.AddDbContext<GameStudioContext>(options => options.UseSqlite(connectionString));

// репозиторій — відкритий узагальнений тип: Repository<T> для будь-якої моделі БД
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IEntityRepository<>), typeof(Repository<>));

// перетворювачі «доменна модель ↔ модель БД»
builder.Services.AddScoped<IModelMapper<GameProject, Db.GameProjectModel>, GameProjectMapper>();
builder.Services.AddScoped<IModelMapper<BugReport, Db.BugReportModel>, BugReportMapper>();
builder.Services.AddScoped<IModelMapper<Employee, Db.EmployeeModel>, EmployeeMapper>();
builder.Services.AddScoped<IModelMapper<Programmer, Db.ProgrammerModel>, ProgrammerMapper>();
builder.Services.AddScoped<IModelMapper<Artist, Db.ArtistModel>, ArtistMapper>();

// ---------- Рівень бізнес-логіки (GameStudio.Common): асинхронний CRUD-сервіс з ЛР3 ----------
builder.Services.AddScoped<ICrudServiceAsync<GameProject>, CrudServiceAsync<GameProject, Db.GameProjectModel>>();
builder.Services.AddScoped<ICrudServiceAsync<BugReport>, CrudServiceAsync<BugReport, Db.BugReportModel>>();
builder.Services.AddScoped<ICrudServiceAsync<Employee>, CrudServiceAsync<Employee, Db.EmployeeModel>>();
builder.Services.AddScoped<ICrudServiceAsync<Programmer>, CrudServiceAsync<Programmer, Db.ProgrammerModel>>();
builder.Services.AddScoped<ICrudServiceAsync<Artist>, CrudServiceAsync<Artist, Db.ArtistModel>>();

// ---------- ЛР5: ASP.NET Core Identity — користувачі, ролі, токени ----------
// AddIdentityApiEndpoints реєструє UserManager, SignInManager, схему автентифікації Bearer (токени доступу
// та оновлення) і сервіси для вбудованих кінцевих точок /register, /login, /refresh тощо.
builder.Services
    .AddIdentityApiEndpoints<StudioUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()                          // RoleManager<IdentityRole> і ролі в токені
    .AddUserManager<StudioUserManager>()               // роль Tester для кожного нового користувача
    .AddEntityFrameworkStores<GameStudioContext>();    // користувачі та ролі зберігаються в БД з ЛР3

// час життя токена доступу; токен оновлення (refreshToken) дійсний 14 днів
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme,
    options => options.BearerTokenExpiration = TimeSpan.FromHours(1));

builder.Services.AddAuthorization();

// на хостингу (Render) HTTPS завершується на проксі-сервері: беремо схему й IP клієнта із заголовків X-Forwarded-*
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks();

// ---------- Рівень представлення: контролери Web API + Swagger ----------
builder.Services.AddControllers(options =>
    {
        // назви полів у помилках перевірки — такі самі, як у JSON (camelCase): "title", а не "Title"
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());

        // відповіді лише у JSON (без text/plain); помилки — application/problem+json
        options.OutputFormatters.RemoveType<StringOutputFormatter>();
    })
    .AddJsonOptions(options =>
    {
        // переліки (статус, критичність, грейд) передаються рядками: "Released", а не 4
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// усі помилки (400, 404, 409, 500) повертаються у форматі ProblemDetails (application/problem+json)
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        ProblemDetails problem = context.ProblemDetails;
        if (problem is HttpValidationProblemDetails)
        {
            problem.Title = "Помилка перевірки даних";
        }
        else if (problem.Status == StatusCodes.Status401Unauthorized && problem.Detail is null)
        {
            problem.Title = "Потрібна автентифікація";
            problem.Detail = "Додайте заголовок Authorization: Bearer <accessToken> з відповіді POST /api/auth/login.";
        }
        else if (problem.Status == StatusCodes.Status403Forbidden)
        {
            problem.Title = "Доступ заборонено";
            problem.Detail = "Роль користувача не дозволяє виконати цю дію.";
        }
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GameStudio REST API",
        Version = "v1",
        Description = "REST API ігрової студії — проєкти, баги, програмісти та художники (CRUD над БД з ЛР3). " +
                      "ЛР5: реєстрація й вхід (/api/auth), доступ за токеном Bearer і ролями Admin, Manager, Developer, Tester."
    });

    // кнопка «Authorize»: токен з відповіді /api/auth/login додається до запитів у заголовку Authorization: Bearer <токен>
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Токен доступу (accessToken) з відповіді POST /api/auth/login."
    });

    // замок і відповіді 401/403 лише для захищених кінцевих точок; у описі — ролі, яким дозволено доступ
    options.OperationFilter<AuthorizationOperationFilter>();

    // XML-коментарі контролерів (/// <summary>) показуються на сторінці Swagger
    string xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xmlFile);
});

var app = builder.Build();

// застосовуємо міграції (ЛР3 + таблиці Identity з ЛР5) і створюємо ролі та адміністратора
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    await services.GetRequiredService<GameStudioContext>().Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(
        services.GetRequiredService<RoleManager<IdentityRole>>(),
        services.GetRequiredService<UserManager<StudioUser>>(),
        app.Configuration["Seed:AdminEmail"],
        app.Configuration["Seed:AdminPassword"],
        services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder"));
}

app.UseForwardedHeaders();

// необроблені винятки → 500 у форматі ProblemDetails (у режимі Development — сторінка з деталями помилки)
app.UseExceptionHandler();

// відповіді без тіла (401 Unauthorized, 403 Forbidden, 404 для невідомої адреси) теж отримують тіло ProblemDetails
app.UseStatusCodePages();

// Swagger доступний і на сервері: через нього виконуються запити до розгорнутого застосунку
app.UseSwagger();
app.UseSwaggerUI(options => options.DocumentTitle = "GameStudio REST API");

app.UseHttpsRedirection();

// автентифікація (хто робить запит — за токеном із заголовка Authorization), потім авторизація (чи дозволено)
app.UseAuthentication();
app.UseAuthorization();

// вбудовані кінцеві точки ASP.NET Core Identity: /api/auth/register, /api/auth/login, /api/auth/refresh, /api/auth/manage/info …
app.MapGroup("/api/auth")
    .WithTags("Auth")
    .MapIdentityApi<StudioUser>();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

// потрібно для інтеграційних тестів (WebApplicationFactory<Program>)
public partial class Program
{
}
