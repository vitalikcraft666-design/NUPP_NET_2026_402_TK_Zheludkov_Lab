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
    public class ArtistsController : Controller
    {
        private readonly GameStudioContext _context;

        public ArtistsController(GameStudioContext context)
        {
            _context = context;
        }

        // GET: Artists?page=2 — список посторінково (по {Paging.PageSize} записів)
        public async Task<IActionResult> Index(int page = 1)
        {
            return View(await _context.Artists.OrderBy(a => a.Id).ToPageAsync(page, ViewData));
        }

        // GET: Artists/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artistModel = await _context.Artists
                .FirstOrDefaultAsync(m => m.Id == id);
            if (artistModel == null)
            {
                return NotFound();
            }

            return View(artistModel);
        }

        // GET: Artists/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Artists/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Specialization,MainTool,ArtworksCompleted,Id,PublicId,FullName,HireDate,BaseSalary")] ArtistModel artistModel)
        {
            if (ModelState.IsValid)
            {
                artistModel.PublicId = Guid.NewGuid();
                _context.Add(artistModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(artistModel);
        }

        // GET: Artists/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artistModel = await _context.Artists.FindAsync(id);
            if (artistModel == null)
            {
                return NotFound();
            }
            return View(artistModel);
        }

        // POST: Artists/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Specialization,MainTool,ArtworksCompleted,Id,PublicId,FullName,HireDate,BaseSalary")] ArtistModel artistModel)
        {
            if (id != artistModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(artistModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArtistModelExists(artistModel.Id))
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
            return View(artistModel);
        }

        // GET: Artists/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artistModel = await _context.Artists
                .FirstOrDefaultAsync(m => m.Id == id);
            if (artistModel == null)
            {
                return NotFound();
            }

            return View(artistModel);
        }

        // POST: Artists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var artistModel = await _context.Artists.FindAsync(id);
            if (artistModel != null)
            {
                _context.Artists.Remove(artistModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ArtistModelExists(int id)
        {
            return _context.Artists.Any(e => e.Id == id);
        }
    }
}
