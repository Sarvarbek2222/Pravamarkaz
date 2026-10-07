using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Data;
using PravaMarkaz.Models;
using PravaMarkaz.Services;

namespace PravaMarkaz.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly Audit _audit;
    private readonly CurrentUser _me;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AccountController(AppDbContext db, Audit audit, CurrentUser me)
    {
        _db = db;
        _audit = audit;
        _me = me;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == vm.Username.Trim());
        var valid = user != null && _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password) != PasswordVerificationResult.Failed;
        if (!valid || !user!.IsActive)
        {
            // Parolni tanlab topishni sekinlashtirish uchun
            await Task.Delay(1000);
            ModelState.AddModelError("", valid ? "Hisobingiz bloklangan. Egasiga murojaat qiling." : "Login yoki parol noto'g'ri");
            return View(vm);
        }
        if (user.Role == UserRole.Manager && user.CenterId == null)
        {
            ModelState.AddModelError("", "Sizga markaz biriktirilmagan. Egasiga murojaat qiling.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(CurrentUser.FullNameClaim, string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName),
            new(CurrentUser.StampClaim, user.SecurityStamp)
        };
        if (user.CenterId != null) claims.Add(new Claim(CurrentUser.CenterClaim, user.CenterId.Value.ToString()));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true });

        user.LastLoginAt = DateTime.Now;
        _audit.Log("Tizimga kirdi", null, user.CenterId, user.Username);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Denied() => View();

    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _db.Users.FindAsync(_me.Id);
        if (user == null) return RedirectToAction(nameof(Login));

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, vm.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(vm.CurrentPassword), "Joriy parol noto'g'ri");
            return View(vm);
        }

        user.PasswordHash = _hasher.HashPassword(user, vm.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        _audit.Log("Parolini o'zgartirdi");
        await _db.SaveChangesAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Ok"] = "Parol o'zgartirildi. Yangi parol bilan kiring.";
        return RedirectToAction(nameof(Login));
    }
}
