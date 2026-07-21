using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Attributes;

namespace Itransition.Controllers
{
    [Authorize(Roles = "Administrator,Recruiter")]
    public class AttributeOptionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttributeOptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AttributeOptions
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.AttributeOptions.Include(a => a.AttributeDefinition);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: AttributeOptions/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attributeOption = await _context.AttributeOptions
                .Include(a => a.AttributeDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attributeOption == null)
            {
                return NotFound();
            }

            return View(attributeOption);
        }

        // GET: AttributeOptions/Create
        public IActionResult Create()
        {
            ViewData["AttributeDefinitionId"] = new SelectList(_context.AttributeDefinitions, "Id", "Category");
            return View();
        }

        // POST: AttributeOptions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Value,AttributeDefinitionId")] AttributeOption attributeOption)
        {
            if (ModelState.IsValid)
            {
                attributeOption.Id = Guid.NewGuid();
                _context.Add(attributeOption);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AttributeDefinitionId"] = new SelectList(_context.AttributeDefinitions, "Id", "Category", attributeOption.AttributeDefinitionId);
            return View(attributeOption);
        }

        // GET: AttributeOptions/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attributeOption = await _context.AttributeOptions.FindAsync(id);
            if (attributeOption == null)
            {
                return NotFound();
            }
            ViewData["AttributeDefinitionId"] = new SelectList(_context.AttributeDefinitions, "Id", "Category", attributeOption.AttributeDefinitionId);
            return View(attributeOption);
        }

        // POST: AttributeOptions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Value,AttributeDefinitionId")] AttributeOption attributeOption)
        {
            if (id != attributeOption.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(attributeOption);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AttributeOptionExists(attributeOption.Id))
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
            ViewData["AttributeDefinitionId"] = new SelectList(_context.AttributeDefinitions, "Id", "Category", attributeOption.AttributeDefinitionId);
            return View(attributeOption);
        }

        // GET: AttributeOptions/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attributeOption = await _context.AttributeOptions
                .Include(a => a.AttributeDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attributeOption == null)
            {
                return NotFound();
            }

            return View(attributeOption);
        }

        // POST: AttributeOptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var attributeOption = await _context.AttributeOptions.FindAsync(id);
            if (attributeOption != null)
            {
                _context.AttributeOptions.Remove(attributeOption);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AttributeOptionExists(Guid id)
        {
            return _context.AttributeOptions.Any(e => e.Id == id);
        }
    }
}
