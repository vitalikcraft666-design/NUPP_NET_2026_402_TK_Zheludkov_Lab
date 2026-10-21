using System.Globalization;
using System.Text;
using GameStudio.ConsoleApp.Demos;

// UTF-8 — щоб українські літери коректно відображались у консолі Windows;
// культура uk-UA — щоб числа форматувались по-українськи (45 000,00).
// DefaultThreadCurrentCulture потрібна, бо в ЛР2 код виконується і в потоках пулу (Parallel, Task).
Console.OutputEncoding = Encoding.UTF8;
CultureInfo ukrainian = CultureInfo.GetCultureInfo("uk-UA");
CultureInfo.DefaultThreadCurrentCulture = ukrainian;
CultureInfo.CurrentCulture = ukrainian;

// За замовчуванням запускається демонстрація ЛР2.
// Демонстрація ЛР1: dotnet run --project GameStudio.Console -- lab1
if (args.Contains("lab1", StringComparer.OrdinalIgnoreCase))
{
    Lab1Demo.Run();
}
else
{
    await Lab2Demo.RunAsync();
}
