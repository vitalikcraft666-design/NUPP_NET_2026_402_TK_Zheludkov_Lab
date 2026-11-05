using System.Globalization;
using GameStudio.Infrastructure;
using GameStudio.MVC.Metadata;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ЛР4, додаткове завдання: вебзастосунок ASP.NET MVC працює з тим самим контекстом БД і моделями, що й ЛР3.
// Порожній рядок підключення — файл Database/gamestudio.db поруч із GameStudio.sln.
string? connectionString = builder.Configuration.GetConnectionString("GameStudio");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = $"Data Source={DatabaseLocator.GetDefaultDatabasePath()}";
}

// контекст БД реєструється у контейнері впровадження залежностей і передається в конструктори контролерів
builder.Services.AddDbContext<GameStudioContext>(options => options.UseSqlite(connectionString));

builder.Services.AddControllersWithViews(options =>
{
    // підписи українською для властивостей моделей БД (див. Metadata/UkrainianDisplayMetadataProvider.cs)
    options.ModelMetadataDetailsProviders.Add(new UkrainianDisplayMetadataProvider());

    // повідомлення прив'язки моделі українською
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(field => $"Поле «{field}» має бути числом.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(value => $"Значення «{value}» не може бути порожнім.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((value, field) => $"Значення «{value}» некоректне для поля «{field}».");
});

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<GameStudioContext>().Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// українська культура (дати 05.11.2026, числа 1 234 567), але з крапкою як десятковим роздільником:
// так числа однаково розуміють і перевірка в браузері (jQuery Validation), і прив'язка моделі на сервері
var ukrainian = (CultureInfo)CultureInfo.GetCultureInfo("uk-UA").Clone();
ukrainian.NumberFormat.NumberDecimalSeparator = ".";
ukrainian.NumberFormat.CurrencyDecimalSeparator = ".";
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ukrainian),
    SupportedCultures = new[] { ukrainian },
    SupportedUICultures = new[] { ukrainian }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
