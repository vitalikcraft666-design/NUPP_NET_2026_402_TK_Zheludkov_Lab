namespace GameStudio.Common.Interfaces;

/// <summary>
/// ЛР3: перетворення між доменною моделлю (GameStudio.Common.Models) і моделлю бази даних.
/// Перетворення в модель БД асинхронне, бо може потребувати пошуку пов'язаних записів (зовнішніх ключів).
/// </summary>
public interface IModelMapper<TDomain, TModel>
{
    TDomain ToDomain(TModel model);

    Task<TModel> ToModelAsync(TDomain domain);

    Task UpdateModelAsync(TModel model, TDomain domain);
}
