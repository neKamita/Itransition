using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Attributes;
using Itransition.ViewModel;

using Microsoft.AspNetCore.Authorization;

namespace Itransition.Controllers
{
    [Authorize(Roles = "Administrator,Recruiter")]
    public class AttributeDefinitionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttributeDefinitionsController> _logger;

        public AttributeDefinitionsController(
            ApplicationDbContext context,
            ILogger<AttributeDefinitionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: AttributeDefinitions
        public async Task<IActionResult> Index()
        {
            return View(await _context.AttributeDefinitions
                .AsNoTracking()
                .Include(attribute => attribute.Category)
                .OrderBy(attribute => attribute.Category.SortOrder)
                .ThenBy(attribute => attribute.Name)
                .ToListAsync());
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
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attributeDefinition == null)
            {
                return NotFound();
            }

            return View(attributeDefinition);
        }

        // GET: AttributeDefinitions/Create
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesAsync();
            return View();
        }

        // POST: AttributeDefinitions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttributeDefinitionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var attributeDefinition = new AttributeDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = model.Name,
                    CategoryId = model.CategoryId,
                    Description = model.Description,
                    DataType = model.DataType,
                    IsBuiltIn = false
                };

                _context.Add(attributeDefinition);
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(nameof(model.Name), "An attribute with this name already exists.");
                }
            }

            await PopulateCategoriesAsync(model.CategoryId);
            return View(model);
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

            var model = new AttributeDefinitionViewModel
            {
                Id = attributeDefinition.Id,
                Name = attributeDefinition.Name,
                CategoryId = attributeDefinition.CategoryId,
                Description = attributeDefinition.Description,
                DataType = attributeDefinition.DataType,
                IsBuiltIn = attributeDefinition.IsBuiltIn,
                Version = attributeDefinition.Version
            };

            await PopulateCategoriesAsync(model.CategoryId);
            return View(model);
        }

        // POST: AttributeDefinitions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AttributeDefinitionViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync(model.CategoryId);
                return View(model);
            }

            var definitionToUpdate = await _context.AttributeDefinitions.FindAsync(id);
            if (definitionToUpdate is null)
            {
                return NotFound();
            }

            definitionToUpdate.Name = model.Name;
            definitionToUpdate.CategoryId = model.CategoryId;
            definitionToUpdate.Description = model.Description;
            if (!definitionToUpdate.IsBuiltIn)
            {
                definitionToUpdate.DataType = model.DataType;
            }
            _context.Entry(definitionToUpdate).Property(item => item.Version).OriginalValue = model.Version;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.AttributeDefinitions.AnyAsync(item => item.Id == id))
                {
                    return NotFound();
                }

                var databaseValues = await _context.Entry(definitionToUpdate).GetDatabaseValuesAsync();
                model.Version = databaseValues?.GetValue<uint>(nameof(AttributeDefinition.Version))
                    ?? model.Version;
                ModelState.AddModelError(string.Empty, "This attribute was changed by another recruiter. Review your values and submit again.");
                await PopulateCategoriesAsync(model.CategoryId);
                return View(model);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(model.Name), "An attribute with this name already exists.");
                await PopulateCategoriesAsync(model.CategoryId);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: AttributeDefinitions/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attributeDefinition = await _context.AttributeDefinitions
                .Include(attribute => attribute.Category)
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
        public async Task<IActionResult> DeleteConfirmed(Guid id, uint version)
        {
            var attributeDefinition = await _context.AttributeDefinitions.FindAsync(id);
            if (attributeDefinition?.IsBuiltIn == true)
            {
                TempData["AttributeError"] = "Built-in profile attributes are protected and cannot be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (attributeDefinition != null)
            {
                _context.Entry(attributeDefinition).Property(item => item.Version).OriginalValue = version;
                _context.AttributeDefinitions.Remove(attributeDefinition);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["AttributeError"] = "The attribute changed before deletion. Review it and try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(
            List<Guid>? selectedIds,
            Dictionary<Guid, uint>? selectedVersions)
        {
            if (selectedIds is null || selectedIds.Count == 0)
            {
                TempData["AttributeNotice"] = "Select at least one attribute to delete.";
                return RedirectToAction(nameof(Index));
            }

            var ids = selectedIds.Distinct().ToList();
            var attributes = await _context.AttributeDefinitions
                .Where(attribute => ids.Contains(attribute.Id))
                .ToListAsync();

            if (attributes.Count != ids.Count)
            {
                TempData["AttributeError"] = "One or more selected attributes no longer exist.";
                return RedirectToAction(nameof(Index));
            }

            if (attributes.Any(attribute => attribute.IsBuiltIn))
            {
                TempData["AttributeError"] = "Built-in profile attributes are protected. Remove them from the selection and try again.";
                return RedirectToAction(nameof(Index));
            }

            if (selectedVersions is null || ids.Any(id => !selectedVersions.ContainsKey(id)))
            {
                return BadRequest("A version is required for every selected attribute.");
            }

            foreach (var attribute in attributes)
            {
                _context.Entry(attribute).Property(item => item.Version).OriginalValue = selectedVersions[attribute.Id];
            }

            try
            {
                _context.AttributeDefinitions.RemoveRange(attributes);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["AttributeError"] = "At least one selected attribute changed. Nothing was deleted; reload the list and try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Bulk deletion of attribute definitions failed for user {UserId}",
                    User.Identity?.Name);
                TempData["AttributeError"] = "The selected attributes could not be deleted because related data still uses them.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation(
                "User {UserId} deleted {AttributeCount} attribute definition(s)",
                User.Identity?.Name,
                attributes.Count);
            TempData["AttributeSuccess"] = $"Deleted {attributes.Count} attribute(s).";
            return RedirectToAction(nameof(Index));
        }


        private async Task PopulateCategoriesAsync(Guid? selectedCategoryId = null)
        {
            var categories = await _context.AttributeCategories
                .AsNoTracking()
                .OrderBy(category => category.SortOrder)
                .ThenBy(category => category.Name)
                .ToListAsync();
            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", selectedCategoryId);
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
        public async Task<IActionResult> RemoveOption(Guid optionId, Guid attributeId, uint version)
        {
            var opt = await _context.AttributeOptions.FindAsync(optionId);
            if (opt != null && opt.AttributeDefinitionId == attributeId)
            {
                _context.Entry(opt).Property(item => item.Version).OriginalValue = version;
                _context.AttributeOptions.Remove(opt);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["AttributeError"] = "The option changed before deletion. Reload the attribute and try again.";
                }
            }
            return RedirectToAction(nameof(Details), new { id = attributeId });
        }
    }
}
