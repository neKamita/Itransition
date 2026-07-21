using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Itransition.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;

namespace Itransition.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public async Task<IActionResult> Index()
    {
        var publicPositions = await _context.Positions
            .Where(p => p.IsPublic)
            .ToListAsync();

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



}
