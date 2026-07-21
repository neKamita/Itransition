using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Profiles;
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

        private bool CandidateProfileExists(Guid id)
        {
            return _context.CandidateProfiles.Any(e => e.Id == id);
        }
    }
}
