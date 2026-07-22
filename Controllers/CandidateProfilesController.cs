using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Profiles;
using Itransition.Models.Cvs;
using Itransition.Models.Attributes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Itransition.ViewModel;

namespace Itransition.Controllers
{
    [Authorize]
    public class CandidateProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CandidateProfilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (myProfile == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Details), new { id = myProfile.Id });
        }


        // GET: CandidateProfiles/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidateProfile = await _context.CandidateProfiles
                .Include(c => c.User)
                .Include(c => c.AttributeValues)
                .ThenInclude(av => av.AttributeDefinition)
                .ThenInclude(a => a.Options)
                .Include(c => c.Projects)
                .ThenInclude(p => p.TechnologyTags)
                .Include(c => c.Cvs)
                .ThenInclude(cv => cv.Position)

                .FirstOrDefaultAsync(m => m.Id == id);
            if (candidateProfile == null)
            {
                return NotFound();
            }

            return View(candidateProfile);
        }

        // GET: CandidateProfiles/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: CandidateProfiles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CandidateProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var candidateProfile = new CandidateProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    FirstName = model.FirstName,
                    User = null!,
                    LastName = model.LastName,
                    Location = model.Location,
                    PersonalPhotoUrl = model.PersonalPhotoUrl
                };

                _context.Add(candidateProfile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: CandidateProfiles/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidateProfile = await _context.CandidateProfiles.FindAsync(id);
            if (candidateProfile == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", candidateProfile.UserId);
            return View(candidateProfile);
        }

        // POST: CandidateProfiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,UserId,FirstName,LastName,Location,PersonalPhotoUrl,RowVersion")] CandidateProfile candidateProfile)
        {
            if (id != candidateProfile.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(candidateProfile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CandidateProfileExists(candidateProfile.Id))
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
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", candidateProfile.UserId);
            return View(candidateProfile);
        }

        // GET: CandidateProfiles/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidateProfile = await _context.CandidateProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (candidateProfile == null)
            {
                return NotFound();
            }

            return View(candidateProfile);
        }

        // POST: CandidateProfiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var candidateProfile = await _context.CandidateProfiles.FindAsync(id);
            if (candidateProfile != null)
            {
                _context.CandidateProfiles.Remove(candidateProfile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetAttributes()
        {
            var attrs = await _context.AttributeDefinitions
                .Select(a => new { a.Id, a.Name, a.Category })
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .ToListAsync();
            return Json(attrs);
        }

        [HttpPost]
        public async Task<IActionResult> AddAttribute(Guid profileId, Guid attributeDefinitionId)
        {
            if (profileId == Guid.Empty || attributeDefinitionId == Guid.Empty) return BadRequest();
            var value = new UserAttributeValue
            {
                Id = Guid.NewGuid(),
                CandidateProfileId = profileId,
                AttributeDefinitionId = attributeDefinitionId,
                AttributeDefinition = null!,
                CandidateProfile = null!
            };
            _context.UserAttributeValues.Add(value);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = value.Id });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAttribute(Guid id)
        {
            var value = await _context.UserAttributeValues.FindAsync(id);
            if (value != null) {
                _context.UserAttributeValues.Remove(value);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAttributeValue(Guid id, string value)
        {
            var attr = await _context.UserAttributeValues.FindAsync(id);
            if (attr != null) {
                attr.Value = value;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddProject(Guid profileId, string name, string description, DateTime? startDate, DateTime? endDate, string tags)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name is required");
            if (startDate.HasValue && endDate.HasValue && endDate < startDate) return BadRequest("End date cannot be earlier than start date");

            var project = new ProjectProfile
            {
                Id = Guid.NewGuid(),
                CandidateProfileId = profileId,
                CandidateProfile = null!,
                Name = name,
                Description = description,
                StartDate = startDate,
                EndDate = endDate
            };

            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagList = tags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(t => t.Trim())
                                  .Where(t => !string.IsNullOrEmpty(t));
                foreach (var tag in tagList)
                {
                    project.TechnologyTags.Add(new ProjectTechnologyTag { 
                        Id = Guid.NewGuid(), 
                        ProjectProfileId = project.Id, 
                        ProjectProfile = null!, 
                        TagName = tag 
                    });
                }
            }

            _context.ProjectProfiles.Add(project);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var project = await _context.ProjectProfiles.FindAsync(id);
            if (project != null) {
                _context.ProjectProfiles.Remove(project);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        private bool CandidateProfileExists(Guid id)
        {
            return _context.CandidateProfiles.Any(e => e.Id == id);
        }
    }
}
