using System.ComponentModel.DataAnnotations;
using GameStudio.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace GameStudio.MVC.Metadata;

/// <summary>
/// Підписи українською та формати відображення для властивостей моделей БД з ЛР3.
/// Згенеровані сторінки показують підписи через Html.DisplayNameFor / &lt;label asp-for&gt;, тож завдяки цьому
/// провайдеру метаданих не потрібно додавати атрибути [Display] у моделі рівня доступу до даних.
/// Також перекладає стандартне повідомлення атрибута [Required] (зокрема неявного — для властивостей,
/// які не можуть бути null).
/// </summary>
public sealed class UkrainianDisplayMetadataProvider : IDisplayMetadataProvider, IValidationMetadataProvider
{
    // підписи, які залежать від класу моделі
    private static readonly Dictionary<(Type, string), string> SpecificNames = new()
    {
        [(typeof(GameProjectModel), nameof(GameProjectModel.Title))] = "Назва",
        [(typeof(BugReportModel), nameof(BugReportModel.Title))] = "Опис бага"
    };

    // спільні підписи
    private static readonly Dictionary<string, string> Names = new()
    {
        ["Id"] = "Id",
        ["PublicId"] = "Публічний ідентифікатор",
        ["Genre"] = "Жанр",
        ["Engine"] = "Рушій",
        ["Budget"] = "Бюджет, грн",
        ["Spent"] = "Витрачено, грн",
        ["Status"] = "Статус",
        ["StorePage"] = "Сторінка в магазині",
        ["FullName"] = "ПІБ",
        ["HireDate"] = "Дата прийняття",
        ["BaseSalary"] = "Ставка, грн",
        ["PrimaryLanguage"] = "Мова програмування",
        ["Grade"] = "Грейд",
        ["CommitsCount"] = "Комітів",
        ["Specialization"] = "Спеціалізація",
        ["MainTool"] = "Основний інструмент",
        ["ArtworksCompleted"] = "Завершених робіт",
        ["Severity"] = "Критичність",
        ["IsFixed"] = "Виправлено",
        ["CreatedAt"] = "Створено",
        ["GameProject"] = "Проєкт",
        ["GameProjectId"] = "Проєкт",
        ["Assignee"] = "Виконавець",
        ["AssigneeId"] = "Виконавець",
        ["Url"] = "Адреса",
        ["Price"] = "Ціна, грн",
        ["WishlistCount"] = "У списку бажаного",
        ["ReleaseDate"] = "Дата виходу",
        ["Role"] = "Роль",
        ["AssignedAt"] = "Дата призначення"
    };

    private static readonly HashSet<string> MoneyProperties = new() { "Budget", "Spent", "BaseSalary", "Price" };

    private static readonly HashSet<string> DateProperties = new() { "HireDate", "ReleaseDate", "AssignedAt" };

    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        ModelMetadataIdentity key = context.Key;
        if (key.MetadataKind != ModelMetadataKind.Property || key.Name is null || key.ContainerType is null
            || key.ContainerType.Namespace != typeof(IDbModel).Namespace)
        {
            return;
        }

        string name = key.Name;
        string? label = FindSpecificName(key.ContainerType, name) ?? Names.GetValueOrDefault(name);
        if (label is not null)
        {
            context.DisplayMetadata.DisplayName = () => label;
        }

        if (MoneyProperties.Contains(name))
        {
            // 1 672 000 у таблицях; у полі редагування — звичайне число
            context.DisplayMetadata.DisplayFormatString = "{0:N0}";
        }
        else if (DateProperties.Contains(name))
        {
            // поле <input type="date"> і дата без часу в таблицях
            context.DisplayMetadata.DataTypeName = "Date";
            context.DisplayMetadata.DisplayFormatString = "{0:dd.MM.yyyy}";
            context.DisplayMetadata.EditFormatString = "{0:yyyy-MM-dd}";
            context.DisplayMetadata.HasNonDefaultEditFormat = true;
        }
        else if (name == "CreatedAt")
        {
            context.DisplayMetadata.DisplayFormatString = "{0:dd.MM.yyyy HH:mm}";
        }
    }

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        // для значущих типів (decimal, int, enum) ASP.NET сам додає [Required] з англійським текстом,
        // якщо атрибута немає, тому додаємо його явно — з українським повідомленням
        Type type = context.Key.ModelType;
        if (context.Key.MetadataKind == ModelMetadataKind.Property && type.IsValueType
            && Nullable.GetUnderlyingType(type) is null
            && !context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>().Any())
        {
            context.ValidationMetadata.ValidatorMetadata.Add(new RequiredAttribute());
        }

        foreach (RequiredAttribute required in context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>())
        {
            if (required.ErrorMessage is null && required.ErrorMessageResourceName is null)
            {
                required.ErrorMessage = "Поле «{0}» обов'язкове.";
            }
        }
    }

    // шукає підпис для класу або його базового класу (ProgrammerModel → EmployeeModel)
    private static string? FindSpecificName(Type containerType, string name)
    {
        for (Type? type = containerType; type is not null; type = type.BaseType)
        {
            if (SpecificNames.TryGetValue((type, name), out string? label))
            {
                return label;
            }
        }

        return null;
    }
}
