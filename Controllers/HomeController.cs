using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Itransition.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Positions;


namespace Itransition.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public async Task<IActionResult> Index()
    {
        ViewBag.PositionsCount = await _context.Positions.CountAsync();
        ViewBag.CandidatesCount = await _context.CandidateProfiles.CountAsync();
        ViewBag.CvsCount = await _context.Cvs.CountAsync();

        var publicPositions = await _context.Positions
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.CreatedDate)
            .Take(6)
            .ToListAsync();

        var allPositions = await _context.Positions.ToListAsync();
        var allTags = allPositions
            .Where(p => !string.IsNullOrEmpty(p.Tags))
            .SelectMany(p => p.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(t => t.Trim())
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(15)
            .ToDictionary(g => g.Key, g => g.Count());

        ViewBag.PopularTags = allTags;

        return View(publicPositions);
    }

    [Authorize]
    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize(Roles = "Candidate")]
    public IActionResult Candidate()
    {
        return View();
    }

    [Authorize(Roles = "Recruiter")]
    public IActionResult Recruiter()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return View("SearchResults", new List<Position>());
        }
        var results = await _context.Positions
            .Where(p => p.Title.Contains(query) || p.Description.Contains(query) || p.Company.Contains(query) || p.Level.Contains(query))
            .ToListAsync();

        return View("SearchResults", results);
    }



}
