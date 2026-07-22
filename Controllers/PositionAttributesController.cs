using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Positions;
using Microsoft.AspNetCore.Authorization;

namespace Itransition.Controllers
{
    [Authorize(Roles = "Administrator,Recruiter")]
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
            var applicationDbContext = _context.PositionAttributes
                .Include(p => p.AttributeDefinition)
                    .ThenInclude(attribute => attribute.Category)
                .Include(p => p.Position);
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
                    .ThenInclude(attribute => attribute.Category)
                .Include(p => p.Position)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (positionAttribute == null)
            {
                return NotFound();
            }

            return View(positionAttribute);
        }

        // GET: PositionAttributes/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var positionAttribute = await _context.PositionAttributes
                .AsNoTracking()
                .Include(requirement => requirement.Position)
                .Include(requirement => requirement.AttributeDefinition)
                .FirstOrDefaultAsync(requirement => requirement.Id == id);
            if (positionAttribute == null)
            {
                return NotFound();
            }
            ViewData["PositionVersion"] = positionAttribute.Position.Version;
            return View(positionAttribute);
        }

        // POST: PositionAttributes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, int orderIndex, uint version, uint positionVersion)
        {
            if (orderIndex is < 0 or > 1000)
            {
                return BadRequest("Order index must be between 0 and 1000.");
            }

            var requirementToUpdate = await _context.PositionAttributes
                .Include(requirement => requirement.Position)
                .Include(requirement => requirement.AttributeDefinition)
                .FirstOrDefaultAsync(requirement => requirement.Id == id);
            if (requirementToUpdate is null)
            {
                return NotFound();
            }

            requirementToUpdate.OrderIndex = orderIndex;
            requirementToUpdate.Position.UpdatedDate = DateTime.UtcNow;
            _context.Entry(requirementToUpdate).Property(item => item.Version).OriginalValue = version;
            _context.Entry(requirementToUpdate.Position).Property(item => item.Version).OriginalValue = positionVersion;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.PositionAttributes.AnyAsync(item => item.Id == id))
                {
                    return NotFound();
                }

                var databaseValues = await _context.Entry(requirementToUpdate).GetDatabaseValuesAsync();
                requirementToUpdate.Version = databaseValues?.GetValue<uint>(nameof(PositionAttribute.Version)) ?? requirementToUpdate.Version;
                ModelState.AddModelError(string.Empty, "This requirement or position was changed by another recruiter. Reload and try again.");
                ViewData["PositionVersion"] = requirementToUpdate.Position.Version;
                return View(requirementToUpdate);
            }

            return RedirectToAction(nameof(Index));
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
                    .ThenInclude(attribute => attribute.Category)
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
        public async Task<IActionResult> DeleteConfirmed(Guid id, uint version, uint positionVersion)
        {
            var positionAttribute = await _context.PositionAttributes
                .Include(requirement => requirement.Position)
                .FirstOrDefaultAsync(requirement => requirement.Id == id);
            if (positionAttribute != null)
            {
                _context.Entry(positionAttribute).Property(item => item.Version).OriginalValue = version;
                _context.Entry(positionAttribute.Position).Property(item => item.Version).OriginalValue = positionVersion;
                positionAttribute.Position.UpdatedDate = DateTime.UtcNow;
                _context.PositionAttributes.Remove(positionAttribute);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "This requirement changed before deletion. Reload and try again.");
                return positionAttribute is null ? NotFound() : View("Delete", positionAttribute);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
