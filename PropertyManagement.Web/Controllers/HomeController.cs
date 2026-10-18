using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Models;

namespace PropertyManagement.Web.Controllers;

public class HomeController : Controller
{
    // The home page routes each visitor to where they start: the login page,
    // or the main page for their role. Login, registration, and logout all
    // redirect here.
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account");
        }

        if (User.IsInRole(Roles.PropertyManager))
        {
            return RedirectToAction("Index", "ManageApplications");
        }

        if (User.IsInRole(Roles.Applicant))
        {
            return RedirectToAction("Index", "Applications");
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
