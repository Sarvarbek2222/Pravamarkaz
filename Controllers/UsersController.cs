using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Data;
using PravaMarkaz.Models;
using PravaMarkaz.Services;

namespace PravaMarkaz.Controllers;

/// <summary>Foydalanuvchilar (egasi va markaz menejerlari) — faqat egasi boshqaradi.</summary>
[Authorize(Policy = "Owner")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly Audit _audit;
    private readonly CurrentUser _me;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public UsersController(AppDbContext db, Audit audit, CurrentUser me)
    {
        _db = db;
        _audit = audit;
        _me = me;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _db.Users.Include(u => u.Center).OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        await LoadCentersAsync(null);
        return View("Edit", new UserFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserFormViewModel vm)
    {
        if (string.IsNullOrEmpty(vm.Password)) ModelState.AddModelError(nameof(vm.Password), "Parolni kiriting");
        await ValidateAsync(vm);
        if (!ModelState.IsValid)
        {
            await LoadCentersAsync(vm.CenterId);
            return View("Edit", vm);
        }

        var user = new AppUser
        {
            Username = vm.Username.Trim(),
            FullName = vm.FullName.Trim(),
            Role = vm.Role,
            CenterId = vm.Role == UserRole.Manager ? vm.CenterId : null,
            IsActive = vm.IsActive
        };
        user.PasswordHash = _hasher.HashPassword(user, vm.Password!);
        _db.Users.Add(user);
        _audit.Log("Foydalanuvchi qo'shdi", $"{user.FullName} ({user.Username}), {Fmt.Label(user.Role)}", user.CenterId);
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"{user.FullName} qo'shildi";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        await LoadCentersAsync(u.CenterId);
        return View(new UserFormViewModel
        {
            Id = u.Id, FullName = u.FullName, Username = u.Username, Role = u.Role, CenterId = u.CenterId, IsActive = u.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UserFormViewModel vm)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        vm.Id = id;

        if (id == _me.Id && (vm.Role != UserRole.Owner || !vm.IsActive))
            ModelState.AddModelError("", "O'zingizning rolingizni o'zgartira yoki o'zingizni bloklay olmaysiz");
        await ValidateAsync(vm);
        if (!ModelState.IsValid)
        {
            await LoadCentersAsync(vm.CenterId);
            return View(vm);
        }

        var newCenter = vm.Role == UserRole.Manager ? vm.CenterId : null;
        var accessChanged = user.Role != vm.Role || user.CenterId != newCenter || user.IsActive != vm.IsActive
                            || user.Username != vm.Username.Trim() || !string.IsNullOrEmpty(vm.Password);

        user.FullName = vm.FullName.Trim();
        user.Username = vm.Username.Trim();
        user.Role = vm.Role;
        user.CenterId = newCenter;
        user.IsActive = vm.IsActive;
        if (!string.IsNullOrEmpty(vm.Password)) user.PasswordHash = _hasher.HashPassword(user, vm.Password);
        // Huquqlar o'zgarsa foydalanuvchi qayta kirishi kerak bo'ladi
        if (accessChanged) user.SecurityStamp = Guid.NewGuid().ToString("N");

        _audit.Log("Foydalanuvchini tahrirladi", $"{user.FullName} ({user.Username})" + (string.IsNullOrEmpty(vm.Password) ? "" : ", parol yangilandi"), user.CenterId);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Foydalanuvchi saqlandi";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (id == _me.Id)
        {
            TempData["Err"] = "O'zingizni o'chira olmaysiz";
            return RedirectToAction(nameof(Index));
        }

        _db.Users.Remove(user);
        _audit.Log("Foydalanuvchini o'chirdi", $"{user.FullName} ({user.Username})", user.CenterId);
        await _db.SaveChangesAsync();
        TempData["Ok"] = $"{user.FullName} o'chirildi";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(UserFormViewModel vm)
    {
        if (vm.Role == UserRole.Manager && (vm.CenterId == null || !await _db.Centers.AnyAsync(c => c.Id == vm.CenterId)))
            ModelState.AddModelError(nameof(vm.CenterId), "Menejer uchun markazni tanlang");
        if (await _db.Users.AnyAsync(u => u.Id != vm.Id && u.Username == vm.Username.Trim()))
            ModelState.AddModelError(nameof(vm.Username), "Bu login band");
    }

    private async Task LoadCentersAsync(int? selected)
    {
        var centers = await _db.Centers.OrderBy(c => c.Name).Select(c => new { c.Id, c.Name }).ToListAsync();
        ViewBag.Centers = new SelectList(centers, "Id", "Name", selected);
    }
}
