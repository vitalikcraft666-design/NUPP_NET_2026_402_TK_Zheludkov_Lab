using System.Text.Json;
using GameStudio.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameStudio.REST.Controllers;

/// <summary>
/// Базовий клас контролерів API. [ApiController] вмикає автоматичну перевірку моделей (400 Bad Request
/// з описом помилок) і формат ProblemDetails (RFC 9457) для відповідей з помилками.
/// Тут зібрано спільні допоміжні методи, щоб усі контролери повертали помилки однаково.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // читає сутність через CRUD-сервіс; null, якщо її немає (сервіс кидає KeyNotFoundException)
    protected static async Task<T?> FindAsync<T>(ICrudServiceAsync<T> service, Guid id)
        where T : class
    {
        try
        {
            return await service.ReadAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    // 404 Not Found з поясненням, що саме не знайдено
    protected ObjectResult NotFoundProblem(string entityName, Guid id) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Не знайдено",
            detail: $"{entityName} з Id {id} не знайдено.");

    // 409 Conflict — запит суперечить поточному стану ресурсу
    protected ObjectResult ConflictProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: "Конфлікт стану ресурсу", detail: detail);

    // 500 — не вдалося зберегти зміни в БД (ICrudServiceAsync.SaveAsync повернув false)
    protected ObjectResult SaveFailedProblem() =>
        Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Помилка збереження",
            detail: "Не вдалося зберегти зміни в базі даних.");

    // 400 — помилка перевірки конкретного поля (наприклад, посилання на неіснуючий проєкт)
    protected ActionResult FieldValidationProblem(string field, string message)
    {
        // назва поля — як у JSON (camelCase), так само як в автоматичних помилках перевірки
        ModelState.AddModelError(JsonNamingPolicy.CamelCase.ConvertName(field), message);
        return ValidationProblem(ModelState);
    }
}
