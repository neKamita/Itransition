using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Itransition.Models;
using Microsoft.AspNetCore.Authorization;

namespace Itransition.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
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



}
