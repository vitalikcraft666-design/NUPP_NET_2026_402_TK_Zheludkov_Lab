using System.Globalization;
using System.Text;
using GameStudio.ConsoleApp.Demos;

// UTF-8 — щоб українські літери коректно відображались у консолі Windows;
// культура uk-UA — щоб числа форматувались по-українськи (45 000,00).
// DefaultThreadCurrentCulture потрібна, бо код виконується і в потоках пулу (Parallel, Task).
Console.OutputEncoding = Encoding.UTF8;
CultureInfo ukrainian = CultureInfo.GetCultureInfo("uk-UA");
CultureInfo.DefaultThreadCurrentCulture = ukrainian;
CultureInfo.CurrentCulture = ukrainian;

// За замовчуванням запускається демонстрація ЛР3 (база даних SQLite).
// Інші демонстрації: dotnet run --project GameStudio.Console -- lab1 | lab2 | nosql (MongoDB, додаткове завдання ЛР3)
// Повторне заповнення БД:  dotnet run --project GameStudio.Console -- --reset
bool HasArg(string name) => args.Contains(name, StringComparer.OrdinalIgnoreCase);

if (HasArg("lab1"))
{
    Lab1Demo.Run();
}
else if (HasArg("lab2"))
{
    await Lab2Demo.RunAsync();
}
else if (HasArg("nosql"))
{
    await NosqlDemo.RunAsync();
}
else
{
    await Lab3Demo.RunAsync(reset: HasArg("--reset"));
}
