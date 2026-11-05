using System.Diagnostics;
using GameStudio.Common.Models;
using GameStudio.Infrastructure;
using GameStudio.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStudio.MVC.Controllers;

public class HomeController : Controller
{
    private readonly GameStudioContext _context;

    // контекст БД з ЛР3 передається через впровадження залежностей
    public HomeController(GameStudioContext context)
    {
        _context = context;
    }

    // головна сторінка — коротка статистика з БД і посилання на розділи
    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            Projects = await _context.GameProjects.CountAsync(),
            ReleasedProjects = await _context.GameProjects.CountAsync(p => p.Status == ProjectStatus.Released),
            Programmers = await _context.Programmers.CountAsync(),
            Artists = await _context.Artists.CountAsync(),
            Bugs = await _context.BugReports.CountAsync(),
            OpenBugs = await _context.BugReports.CountAsync(b => !b.IsFixed),
            StorePages = await _context.StorePages.CountAsync()
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
