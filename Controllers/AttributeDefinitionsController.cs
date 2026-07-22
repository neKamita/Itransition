using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Attributes;

using Microsoft.AspNetCore.Authorization;

namespace Itransition.Controllers
{
    [Authorize(Roles = "Administrator,Recruiter")]
    public class AttributeDefinitionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttributeDefinitionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AttributeDefinitions
        public async Task<IActionResult> Index()
        {
            return View(await _context.AttributeDefinitions.ToListAsync());
        }

        // GET: AttributeDefinitions/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attributeDefinition = await _context.AttributeDefinitions
                .Include(m => m.Options)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attributeDefinition == null)
            {
                return NotFound();
            }

            return View(attributeDefinition);
        }

        // GET: AttributeDefinitions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AttributeDefinitions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Category,Description,DataType,RowVersion")] AttributeDefinition attributeDefinition)
        {
            if (ModelState.IsValid)
            {
                attributeDefinition.Id = Guid.NewGuid();
                _context.Add(attributeDefinition);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(attributeDefinition);
        }

        // GET: AttributeDefinitions/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attributeDefinition = await _context.AttributeDefinitions.FindAsync(id);
            if (attributeDefinition == null)
            {
                return NotFound();
            }
            return View(attributeDefinition);
        }

        // POST: AttributeDefinitions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Category,Description,DataType,RowVersion")] AttributeDefinition attributeDefinition)
        {
            if (id != attributeDefinition.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(attributeDefinition);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AttributeDefinitionExists(attributeDefinition.Id))
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
            return View(attributeDefinition);
        }

        // GET: AttributeDefinitions/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attributeDefinition = await _context.AttributeDefinitions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attributeDefinition == null)
            {
                return NotFound();
            }

            return View(attributeDefinition);
        }

        // POST: AttributeDefinitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var attributeDefinition = await _context.AttributeDefinitions.FindAsync(id);
            if (attributeDefinition != null)
            {
                _context.AttributeDefinitions.Remove(attributeDefinition);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AttributeDefinitionExists(Guid id)
        {
            return _context.AttributeDefinitions.Any(e => e.Id == id);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOption(Guid attributeId, string optionValue)
        {
            if (!string.IsNullOrWhiteSpace(optionValue))
            {
                var attr = await _context.AttributeDefinitions.FindAsync(attributeId);
                if (attr != null && attr.DataType == AttributeDataType.Dropdown)
                {
                    var opt = new AttributeOption { Id = Guid.NewGuid(), AttributeDefinitionId = attributeId, Value = optionValue.Trim() };
                    _context.AttributeOptions.Add(opt);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Details), new { id = attributeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveOption(Guid optionId, Guid attributeId)
        {
            var opt = await _context.AttributeOptions.FindAsync(optionId);
            if (opt != null && opt.AttributeDefinitionId == attributeId)
            {
                _context.AttributeOptions.Remove(opt);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = attributeId });
        }
    }
}
