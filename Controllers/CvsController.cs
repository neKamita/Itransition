using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Cvs;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

namespace Itransition.Controllers
{
    [Authorize]
    public class CvsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CvsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cvs
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Cvs.Include(c => c.CandidateProfile).Include(c => c.Position);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Cvs/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cv = await _context.Cvs
                .Include(c => c.CandidateProfile)
                .Include(c => c.Position)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cv == null)
            {
                return NotFound();
            }

            return View(cv);
        }

        // GET: Cvs/Create
        public IActionResult Create()
        {
            ViewData["CandidateProfileId"] = new SelectList(_context.CandidateProfiles, "Id", "Id");
            ViewData["PositionId"] = new SelectList(_context.Positions, "Id", "Title");
            return View();
        }

        // POST: Cvs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CandidateProfileId,PositionId,Status,LikesCount,DislikesCount,CreatedDate,UpdatedDate,RowVersion")] Cv cv)
        {
            if (ModelState.IsValid)
            {
                cv.Id = Guid.NewGuid();
                _context.Add(cv);
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var myProfile = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == currentUserId);
                if (myProfile != null)
                {
                    cv.CandidateProfileId = myProfile.Id;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CandidateProfileId"] = new SelectList(_context.CandidateProfiles, "Id", "Id", cv.CandidateProfileId);
            ViewData["PositionId"] = new SelectList(_context.Positions, "Id", "Id", cv.PositionId);
            return View(cv);
        }

        // GET: Cvs/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cv = await _context.Cvs.FindAsync(id);
            if (cv == null)
            {
                return NotFound();
            }
            ViewData["CandidateProfileId"] = new SelectList(_context.CandidateProfiles, "Id", "Id", cv.CandidateProfileId);
            ViewData["PositionId"] = new SelectList(_context.Positions, "Id", "Id", cv.PositionId);
            return View(cv);
        }

        // POST: Cvs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CandidateProfileId,PositionId,Status,LikesCount,DislikesCount,CreatedDate,UpdatedDate,RowVersion")] Cv cv)
        {
            if (id != cv.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cv);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CvExists(cv.Id))
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
            ViewData["CandidateProfileId"] = new SelectList(_context.CandidateProfiles, "Id", "Id", cv.CandidateProfileId);
            ViewData["PositionId"] = new SelectList(_context.Positions, "Id", "Id", cv.PositionId);
            return View(cv);
        }

        // GET: Cvs/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cv = await _context.Cvs
                .Include(c => c.CandidateProfile)
                .Include(c => c.Position)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cv == null)
            {
                return NotFound();
            }

            return View(cv);
        }

        // POST: Cvs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var cv = await _context.Cvs.FindAsync(id);
            if (cv != null)
            {
                _context.Cvs.Remove(cv);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CvExists(Guid id)
        {
            return _context.Cvs.Any(e => e.Id == id);
        }

          [HttpPost]
    [Authorize(Roles="Administrator,Recruiter")]
    public async Task<IActionResult> Like(Guid id)
    {
        var cv = await _context.Cvs.FindAsync(id);
        if(cv !=null){
            cv.LikesCount++;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new {id});
    }
    }


}
