using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameStudio.Infrastructure;
using GameStudio.Infrastructure.Models;

namespace GameStudio.MVC.Controllers
{
    public class BugsController : Controller
    {
        private readonly GameStudioContext _context;

        public BugsController(GameStudioContext context)
        {
            _context = context;
        }

        // GET: Bugs?page=2 — список посторінково (по {Paging.PageSize} записів)
        public async Task<IActionResult> Index(int page = 1)
        {
            var gameStudioContext = _context.BugReports.Include(b => b.Assignee).Include(b => b.GameProject);
            return View(await gameStudioContext.OrderBy(b => b.Id).ToPageAsync(page, ViewData));
        }

        // GET: Bugs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bugReportModel = await _context.BugReports
                .Include(b => b.Assignee)
                .Include(b => b.GameProject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bugReportModel == null)
            {
                return NotFound();
            }

            return View(bugReportModel);
        }

        // GET: Bugs/Create
        public IActionResult Create()
        {
            ViewData["AssigneeId"] = new SelectList(_context.Programmers.OrderBy(p => p.FullName), "Id", "FullName");
            ViewData["GameProjectId"] = new SelectList(_context.GameProjects.OrderBy(p => p.Title), "Id", "Title");
            return View();
        }

        // POST: Bugs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Severity,IsFixed,GameProjectId,AssigneeId")] BugReportModel bugReportModel)
        {
            ModelState.Remove(nameof(BugReportModel.GameProject));
            if (ModelState.IsValid)
            {
                bugReportModel.PublicId = Guid.NewGuid();
                bugReportModel.CreatedAt = DateTime.Now;
                _context.Add(bugReportModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AssigneeId"] = new SelectList(_context.Programmers.OrderBy(p => p.FullName), "Id", "FullName", bugReportModel.AssigneeId);
            ViewData["GameProjectId"] = new SelectList(_context.GameProjects.OrderBy(p => p.Title), "Id", "Title", bugReportModel.GameProjectId);
            return View(bugReportModel);
        }

        // GET: Bugs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bugReportModel = await _context.BugReports.FindAsync(id);
            if (bugReportModel == null)
            {
                return NotFound();
            }
            ViewData["AssigneeId"] = new SelectList(_context.Programmers.OrderBy(p => p.FullName), "Id", "FullName", bugReportModel.AssigneeId);
            ViewData["GameProjectId"] = new SelectList(_context.GameProjects.OrderBy(p => p.Title), "Id", "Title", bugReportModel.GameProjectId);
            return View(bugReportModel);
        }

        // POST: Bugs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PublicId,Title,Severity,IsFixed,CreatedAt,GameProjectId,AssigneeId")] BugReportModel bugReportModel)
        {
            if (id != bugReportModel.Id)
            {
                return NotFound();
            }

            // навігаційна властивість не приходить з форми — перевіряється лише зовнішній ключ GameProjectId
            ModelState.Remove(nameof(BugReportModel.GameProject));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bugReportModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BugReportModelExists(bugReportModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AssigneeId"] = new SelectList(_context.Programmers.OrderBy(p => p.FullName), "Id", "FullName", bugReportModel.AssigneeId);
            ViewData["GameProjectId"] = new SelectList(_context.GameProjects.OrderBy(p => p.Title), "Id", "Title", bugReportModel.GameProjectId);
            return View(bugReportModel);
        }

        // GET: Bugs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bugReportModel = await _context.BugReports
                .Include(b => b.Assignee)
                .Include(b => b.GameProject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bugReportModel == null)
            {
                return NotFound();
            }

            return View(bugReportModel);
        }

        // POST: Bugs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bugReportModel = await _context.BugReports.FindAsync(id);
            if (bugReportModel != null)
            {
                _context.BugReports.Remove(bugReportModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BugReportModelExists(int id)
        {
            return _context.BugReports.Any(e => e.Id == id);
        }
    }
}
