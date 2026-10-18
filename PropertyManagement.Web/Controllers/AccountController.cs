using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels.Account;

namespace PropertyManagement.Web.Controllers;

[AllowAnonymous]
public class AccountController(
    RegistrationService registrationService,
    SignInManager<ApplicationUser> signInManager)
    : Controller
{
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await registrationService.RegisterAsync(
            new RegistrationRequest(
                model.Email,
                model.Password,
                model.Role,
                model.FirstName,
                model.LastName,
                model.PhoneNumber,
                model.CurrentAddress));

        if (!result.Succeeded)
        {
            // Field-specific errors go beside fields; general errors go to the summary.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    error.Field ?? string.Empty,
                    error.Message);
            }

            return View(model);
        }

        await signInManager.SignInAsync(result.User!, isPersistent: false);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // UserName is set to the email address at registration.
        var result = await signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            // Deliberately vague: don't reveal whether the email exists.
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
        }

        // Only redirect to local URLs to prevent open-redirect attacks.
        return Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl!)
            : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
