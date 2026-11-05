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
    public class ProjectsController : Controller
    {
        private readonly GameStudioContext _context;

        public ProjectsController(GameStudioContext context)
        {
            _context = context;
        }

        // GET: Projects?page=2 — список посторінково (по {Paging.PageSize} записів)
        public async Task<IActionResult> Index(int page = 1)
        {
            return View(await _context.GameProjects.OrderBy(p => p.Id).ToPageAsync(page, ViewData));
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameProjectModel = await _context.GameProjects
                .Include(p => p.Bugs).ThenInclude(b => b.Assignee)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gameProjectModel == null)
            {
                return NotFound();
            }

            return View(gameProjectModel);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Genre,Engine,Budget,Spent,Status")] GameProjectModel gameProjectModel)
        {
            if (ModelState.IsValid)
            {
                gameProjectModel.PublicId = Guid.NewGuid();
                _context.Add(gameProjectModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(gameProjectModel);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameProjectModel = await _context.GameProjects.FindAsync(id);
            if (gameProjectModel == null)
            {
                return NotFound();
            }
            return View(gameProjectModel);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PublicId,Title,Genre,Engine,Budget,Spent,Status")] GameProjectModel gameProjectModel)
        {
            if (id != gameProjectModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gameProjectModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameProjectModelExists(gameProjectModel.Id))
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
            return View(gameProjectModel);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameProjectModel = await _context.GameProjects
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gameProjectModel == null)
            {
                return NotFound();
            }

            return View(gameProjectModel);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gameProjectModel = await _context.GameProjects.FindAsync(id);
            if (gameProjectModel != null)
            {
                _context.GameProjects.Remove(gameProjectModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GameProjectModelExists(int id)
        {
            return _context.GameProjects.Any(e => e.Id == id);
        }
    }
}
