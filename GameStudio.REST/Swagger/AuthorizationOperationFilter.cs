using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameStudio.REST.Swagger;

/// <summary>
/// ЛР5: позначає на сторінці Swagger захищені кінцеві точки — замок (вимога токена Bearer), відповіді 401 / 403
/// і перелік ролей, яким дозволено виконати запит. Загальнодоступні кінцеві точки ([AllowAnonymous]) не змінюються.
/// </summary>
public sealed class AuthorizationOperationFilter : IOperationFilter
{
    private static readonly OpenApiSecurityScheme BearerScheme = new()
    {
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        IList<object> metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        List<IAuthorizeData> authorize = metadata.OfType<IAuthorizeData>().ToList();
        if (authorize.Count == 0)
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement { [BearerScheme] = Array.Empty<string>() });
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Немає токена або він недійсний." });

        string[] roles = authorize
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries))
            .Distinct()
            .ToArray();

        string access = roles.Length == 0 ? "будь-який автентифікований користувач" : "ролі " + string.Join(", ", roles);
        if (roles.Length > 0)
        {
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Недостатньо прав: роль користувача не дозволяє цю дію." });
        }

        operation.Description = $"🔒 Доступ: {access}." + (string.IsNullOrEmpty(operation.Description) ? "" : " " + operation.Description);
    }
}
