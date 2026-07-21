using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Positions;
using Microsoft.AspNetCore.Authorization;

namespace Itransition.Controllers
{
    public class PositionAttributesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PositionAttributesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PositionAttributes
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PositionAttributes.Include(p => p.AttributeDefinition).Include(p => p.Position);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PositionAttributes/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var positionAttribute = await _context.PositionAttributes
                .Include(p => p.AttributeDefinition)
                .Include(p => p.Position)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (positionAttribute == null)
            {
                return NotFound();
            }

            return View(positionAttribute);
        }

        // GET: PositionAttributes/Create
        public IActionResult Create()
        {
            ViewData["AttributeDefinitionId"] = new SelectList(_context.AttributeDefinitions, "Id", "Category");
            ViewData["PositionId"] = new SelectList(_context.Positions, "Id", "Id");
            return View();
        }

        // POST: PositionAttributes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,PositionId,AttributeDefinitionId,OrderIndex")] PositionAttribute positionAttribute)
        {
            if (ModelState.IsValid)
            {
                positionAttribute.Id = Guid.NewGuid();
                _context.Add(positionAttribute);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AttributeDefinitionId"] = new SelectList(_context.AttributeDefinitions, "Id", "Category", positionAttribute.AttributeDefinitionId);
            ViewData["PositionId"] = new SelectList(_context.Positions, "Id", "Id", positionAttribute.PositionId);
            return View(positionAttribute);
        }

        // GET: PositionAttributes/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var positionAttribute = await _context.PositionAttributes.FindAsync(id);
            if (positionAttribute == null)
            {
                return NotFound();
            }
            ViewData["AttributeDefinitionId"] = new SelectList(_context.AttributeDefinitions, "Id", "Category", positionAttribute.AttributeDefinitionId);
            ViewData["PositionId"] = new SelectList(_context.Positions, "Id", "Id", positionAttribute.PositionId);
            return View(positionAttribute);
        }

        // POST: PositionAttributes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,PositionId,AttributeDefinitionId,OrderIndex")] PositionAttribute positionAttribute)
        {
            if (id != positionAttribute.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(positionAttribute);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PositionAttributeExists(positionAttribute.Id))
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
            ViewData["AttributeDefinitionId"] = new SelectList(_context.AttributeDefinitions, "Id", "Category", positionAttribute.AttributeDefinitionId);
            ViewData["PositionId"] = new SelectList(_context.Positions, "Id", "Id", positionAttribute.PositionId);
            return View(positionAttribute);
        }

        // GET: PositionAttributes/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var positionAttribute = await _context.PositionAttributes
                .Include(p => p.AttributeDefinition)
                .Include(p => p.Position)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (positionAttribute == null)
            {
                return NotFound();
            }

            return View(positionAttribute);
        }

        // POST: PositionAttributes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var positionAttribute = await _context.PositionAttributes.FindAsync(id);
            if (positionAttribute != null)
            {
                _context.PositionAttributes.Remove(positionAttribute);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PositionAttributeExists(Guid id)
        {
            return _context.PositionAttributes.Any(e => e.Id == id);
        }
    }
}
