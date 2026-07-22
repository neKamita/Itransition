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
using System.Security.Claims;

namespace Itransition.Controllers
{
    [Authorize(Roles="Administrator,Recruiter")]
    public class PositionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PositionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Positions
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var positions = await _context.Positions
                .Include(p => p.PositionAccessRules)
                .ToListAsync();

            if (User.IsInRole("Candidate"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var candidate = await _context.CandidateProfiles
                    .Include(c => c.AttributeValues)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (candidate != null)
                {
                    var visiblePositions = new List<Position>();
                    foreach (var pos in positions)
                    {
                        if (pos.IsPublic)
                        {
                            visiblePositions.Add(pos);
                            continue;
                        }

                        bool hasAccess = true;
                        foreach (var rule in pos.PositionAccessRules)
                        {
                            var candidateAttr = candidate.AttributeValues
                                .FirstOrDefault(a => a.AttributeDefinitionId == rule.AttributeDefinitionId);

                            if (candidateAttr == null || string.IsNullOrWhiteSpace(candidateAttr.Value))
                            {
                                hasAccess = false;
                                break;
                            }

                            if (rule.Operator == "==" && candidateAttr.Value != rule.TargetValue) hasAccess = false;
                            if (rule.Operator == "!=" && candidateAttr.Value == rule.TargetValue) hasAccess = false;
                            if (rule.Operator == "CONTAINS" && !candidateAttr.Value.Contains(rule.TargetValue)) hasAccess = false;

                            if (rule.Operator == ">" || rule.Operator == "<")
                            {
                                if (double.TryParse(candidateAttr.Value, out double val1) && double.TryParse(rule.TargetValue, out double val2))
                                {
                                    if (rule.Operator == ">" && val1 <= val2) hasAccess = false;
                                    if (rule.Operator == "<" && val1 >= val2) hasAccess = false;
                                }
                                else
                                {
                                    hasAccess = false;
                                }
                            }

                            if (!hasAccess) break;
                        }

                        if (hasAccess)
                        {
                            visiblePositions.Add(pos);
                        }
                    }
                    return View(visiblePositions);
                }
            }

            return View(positions);
        }

        // GET: Positions/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var position = await _context.Positions
                .Include(p => p.PositionRequiredAttributes)
                    .ThenInclude(pa => pa.AttributeDefinition)
                .Include(p => p.PositionAccessRules)
                    .ThenInclude(pr => pr.AttributeDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (position == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Administrator") || User.IsInRole("Recruiter"))
            {
                ViewBag.AvailableAttributes = await _context.AttributeDefinitions
                    .OrderBy(a => a.Category).ThenBy(a => a.Name)
                    .ToListAsync();
            }

            return View(position);
        }

        // GET: Positions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Positions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Company,Level,MaxProjectInCv,IsPublic,CreatedDate,UpdatedDate,RowVersion")] Position position)
        {
            if (ModelState.IsValid)
            {
                position.Id = Guid.NewGuid();
                _context.Add(position);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(position);
        }

        // GET: Positions/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var position = await _context.Positions.FindAsync(id);
            if (position == null)
            {
                return NotFound();
            }
            return View(position);
        }

        // POST: Positions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Description,Company,Level,MaxProjectInCv,IsPublic,CreatedDate,UpdatedDate,RowVersion")] Position position)
        {
            if (id != position.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(position);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PositionExists(position.Id))
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
            return View(position);
        }

        // GET: Positions/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var position = await _context.Positions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (position == null)
            {
                return NotFound();
            }

            return View(position);
        }

        // POST: Positions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position != null)
            {
                _context.Positions.Remove(position);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PositionExists(Guid id)
        {
            return _context.Positions.Any(e => e.Id == id);
        }
        [HttpPost]
        [Authorize(Roles = "Administrator,Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRequirement(Guid positionId, Guid attributeId)
        {
            if (positionId != Guid.Empty && attributeId != Guid.Empty)
            {
                var req = new PositionAttribute
                {
                    Id = Guid.NewGuid(),
                    PositionId = positionId,
                    AttributeDefinitionId = attributeId,
                    Position = null!,
                    AttributeDefinition = null!
                };
                _context.PositionAttributes.Add(req);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = positionId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRequirement(Guid id, Guid positionId)
        {
            var req = await _context.PositionAttributes.FindAsync(id);
            if (req != null && req.PositionId == positionId)
            {
                _context.PositionAttributes.Remove(req);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = positionId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccessRule(Guid positionId, Guid attributeId, string operatorType, string targetValue)
        {
            if (positionId != Guid.Empty && attributeId != Guid.Empty && !string.IsNullOrWhiteSpace(operatorType))
            {
                var rule = new PositionAccessRule
                {
                    Id = Guid.NewGuid(),
                    PositionId = positionId,
                    AttributeDefinitionId = attributeId,
                    Operator = operatorType,
                    TargetValue = targetValue ?? "",
                    Position = null!,
                    AttributeDefinition = null!
                };
                _context.PositionAccessRules.Add(rule);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = positionId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAccessRule(Guid id, Guid positionId)
        {
            var rule = await _context.PositionAccessRules.FindAsync(id);
            if (rule != null && rule.PositionId == positionId)
            {
                _context.PositionAccessRules.Remove(rule);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = positionId });
        }
    }
}
