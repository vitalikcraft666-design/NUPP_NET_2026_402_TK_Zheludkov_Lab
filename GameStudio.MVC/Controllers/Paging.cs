using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace GameStudio.MVC.Controllers;

/// <summary>
/// Пагінація для сторінок-списків: рахує кількість записів, повертає лише потрібну сторінку (SQL LIMIT/OFFSET)
/// і передає номер сторінки та кількість сторінок у представлення (частковий вигляд _Pager).
/// </summary>
public static class Paging
{
    public const int PageSize = 15;

    public static async Task<List<T>> ToPageAsync<T>(this IQueryable<T> query, int page, ViewDataDictionary viewData)
    {
        int totalCount = await query.CountAsync();
        int totalPages = Math.Max(1, (totalCount + PageSize - 1) / PageSize);
        page = Math.Clamp(page, 1, totalPages);

        viewData["Page"] = page;
        viewData["TotalPages"] = totalPages;
        viewData["TotalCount"] = totalCount;

        return await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
    }
}
