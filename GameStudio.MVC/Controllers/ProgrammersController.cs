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
    public class ProgrammersController : Controller
    {
        private readonly GameStudioContext _context;

        public ProgrammersController(GameStudioContext context)
        {
            _context = context;
        }

        // GET: Programmers?page=2 — список посторінково (по {Paging.PageSize} записів)
        public async Task<IActionResult> Index(int page = 1)
        {
            return View(await _context.Programmers.OrderBy(p => p.Id).ToPageAsync(page, ViewData));
        }

        // GET: Programmers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programmerModel = await _context.Programmers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (programmerModel == null)
            {
                return NotFound();
            }

            return View(programmerModel);
        }

        // GET: Programmers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Programmers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PrimaryLanguage,Grade,CommitsCount,Id,PublicId,FullName,HireDate,BaseSalary")] ProgrammerModel programmerModel)
        {
            if (ModelState.IsValid)
            {
                programmerModel.PublicId = Guid.NewGuid();
                _context.Add(programmerModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(programmerModel);
        }

        // GET: Programmers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programmerModel = await _context.Programmers.FindAsync(id);
            if (programmerModel == null)
            {
                return NotFound();
            }
            return View(programmerModel);
        }

        // POST: Programmers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PrimaryLanguage,Grade,CommitsCount,Id,PublicId,FullName,HireDate,BaseSalary")] ProgrammerModel programmerModel)
        {
            if (id != programmerModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(programmerModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProgrammerModelExists(programmerModel.Id))
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
            return View(programmerModel);
        }

        // GET: Programmers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programmerModel = await _context.Programmers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (programmerModel == null)
            {
                return NotFound();
            }

            return View(programmerModel);
        }

        // POST: Programmers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var programmerModel = await _context.Programmers.FindAsync(id);
            if (programmerModel != null)
            {
                _context.Programmers.Remove(programmerModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProgrammerModelExists(int id)
        {
            return _context.Programmers.Any(e => e.Id == id);
        }
    }
}
