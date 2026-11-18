using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Registering, signing in and signing out (Chapter 25). Everyone may use these pages.
[AllowAnonymous]
[Route("account")]
public class AccountController : Controller
{
    private readonly AccountService _accounts;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AccountService accounts, ILogger<AccountController> logger)
    {
        _accounts = accounts;
        _logger = logger;
    }

    // GET /account/register
    [HttpGet("register")]
    public IActionResult Register() => View(new RegisterInput());

    // POST /account/register
    [HttpPost("register")]
    [EnableRateLimiting("signin")]           // Chapter 27: 5 per minute per IP address
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }
        var user = await _accounts.RegisterAsync(input);
        if (user is null)
        {
            ModelState.AddModelError(nameof(input.Email),
                                     "There is already an account with this email address.");
            return View(input);
        }
        await SignInWithCookieAsync(user, persistent: false);
        TempData["Message"] = $"Welcome to TaskFlow, {user.DisplayName}!";
        return RedirectToAction("Index", "Tasks");
    }

    // GET /account/signin?returnUrl=/tasks/3
    [HttpGet("signin")]
    public IActionResult SignIn(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new SignInInput());
    }

    // POST /account/signin
    [HttpPost("signin")]
    [EnableRateLimiting("signin")]           // Chapter 27: slows down password guessing
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignIn(SignInInput input, string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(input);
        }
        var (result, user) = await _accounts.CheckAsync(input.Email!, input.Password!);
        if (result == SignInCheck.LockedOut)
        {
            ModelState.AddModelError("",
                "Too many wrong passwords. Please wait a few minutes, then try again.");
            return View(input);
        }
        if (result == SignInCheck.Failed || user is null)
        {
            // The same message whether the email exists or not: don't help people guess accounts.
            ModelState.AddModelError("", "Email or password is incorrect.");
            return View(input);
        }

        await SignInWithCookieAsync(user, input.RememberMe);
        _logger.LogInformation("User {UserId} signed in", user.Id);

        // Only ever go back to a page of this site: never to an address someone put in the link.
        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Tasks");
    }

    // POST /account/signout  (a POST, like every change: a link can't sign you out)
    // Named SignOutNow because Controller already has a method called SignOut.
    [HttpPost("signout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignOutNow()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Message"] = "You've signed out.";
        return RedirectToAction("Index", "Home");
    }

    // GET /account/denied  (signed in, but not allowed)
    [HttpGet("denied")]
    public IActionResult Denied() => View();

    // The cookie holds a few facts about the user: "claims".
    private async Task SignInWithCookieAsync(User user, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
        };
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                      new ClaimsPrincipal(identity),
                                      new AuthenticationProperties { IsPersistent = persistent });
    }
}
