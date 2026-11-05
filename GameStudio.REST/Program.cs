using System.Reflection;
using System.Text.Json.Serialization;
using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.Common.Services;
using GameStudio.Infrastructure;
using GameStudio.Infrastructure.Mapping;
using GameStudio.Infrastructure.Repositories;
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
        if (context.ProblemDetails is HttpValidationProblemDetails)
        {
            context.ProblemDetails.Title = "Помилка перевірки даних";
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
        Description = "ЛР4: REST API ігрової студії — проєкти, баги, програмісти та художники (CRUD над БД з ЛР3)."
    });

    // XML-коментарі контролерів (/// <summary>) показуються на сторінці Swagger
    string xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xmlFile);
});

var app = builder.Build();

// застосовуємо міграції ЛР3 (якщо файл БД новий або застарів)
using (IServiceScope scope = app.Services.CreateScope())
{
    GameStudioContext context = scope.ServiceProvider.GetRequiredService<GameStudioContext>();
    await context.Database.MigrateAsync();
}

// необроблені винятки → 500 у форматі ProblemDetails (у режимі Development — сторінка з деталями помилки)
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "GameStudio REST API");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// потрібно для інтеграційних тестів (WebApplicationFactory<Program>)
public partial class Program
{
}
