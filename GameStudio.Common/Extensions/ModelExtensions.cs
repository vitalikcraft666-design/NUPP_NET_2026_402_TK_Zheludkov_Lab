using GameStudio.Common.Models;

namespace GameStudio.Common.Extensions;

/// <summary>
/// Методи розширення для класів моделі.
/// Метод розширення — це статичний метод статичного класу, перший параметр якого позначено словом this.
/// Викликається так, ніби це метод самого об'єкта: employees.TotalMonthlyPayroll().
/// </summary>
public static class ModelExtensions
{
    // метод розширення — сумарний місячний фонд оплати праці для будь-якої колекції працівників
    public static decimal TotalMonthlyPayroll(this IEnumerable<Employee> employees)
    {
        return employees.Sum(employee => employee.CalculateMonthlySalary());
    }

    // метод розширення — короткий опис працівника в один рядок
    public static string ToShortString(this Employee employee)
    {
        return $"{employee.FullName} ({employee.Position})";
    }

    // метод розширення — яку частку бюджету проєкту вже витрачено, у відсотках
    public static decimal BudgetUsagePercent(this GameProject project)
    {
        return project.Budget == 0 ? 0 : Math.Round(project.Spent / project.Budget * 100, 1);
    }

    // метод розширення — чи є серед багів невиправлені критичні
    public static bool HasOpenCriticalBugs(this IEnumerable<BugReport> bugs)
    {
        return bugs.Any(bug => bug.Severity == BugSeverity.Critical && !bug.IsFixed);
    }
}
