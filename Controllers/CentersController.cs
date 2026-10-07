using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Data;
using PravaMarkaz.Models;
using PravaMarkaz.Services;

namespace PravaMarkaz.Controllers;

public class CentersController : Controller
{
    private readonly AppDbContext _db;
    private readonly PhotoStorage _photos;
    private readonly Audit _audit;

    public CentersController(AppDbContext db, PhotoStorage photos, Audit audit)
    {
        _db = db;
        _photos = photos;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var centers = await _db.CenterSummaries().OrderBy(c => c.Name).ToListAsync();
        return View(centers);
    }

    public async Task<IActionResult> Details(int id, StudentListFilter filter)
    {
        var center = await _db.CenterSummaries().FirstOrDefaultAsync(c => c.Id == id);
        if (center == null) return NotFound();

        filter.CenterId = id;
        var students = await _db.StudentListAsync(filter);
        students.ShowCenterColumn = false;
        return View(new CenterDetailsViewModel { Center = center, Students = students });
    }

    [Authorize(Policy = "Owner")]
    public IActionResult Create() => View("Edit", new Center());

    [Authorize(Policy = "Owner")]
    [HttpPost]
    public async Task<IActionResult> Create([Bind("Name,Address,Phone")] Center center)
    {
        if (await _db.Centers.AnyAsync(c => c.Name == center.Name.Trim()))
            ModelState.AddModelError(nameof(center.Name), "Bu nomdagi markaz allaqachon bor");
        if (!ModelState.IsValid) return View("Edit", center);

        center.Name = center.Name.Trim();
        center.CreatedAt = DateTime.Now;
        _db.Centers.Add(center);
        await _db.SaveChangesAsync();
        _audit.Log("Markaz qo'shdi", center.Name, center.Id);
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"\"{center.Name}\" markazi qo'shildi";
        return RedirectToAction(nameof(Details), new { id = center.Id });
    }

    [Authorize(Policy = "Owner")]
    public async Task<IActionResult> Edit(int id)
    {
        var center = await _db.Centers.FindAsync(id);
        return center == null ? NotFound() : View(center);
    }

    [Authorize(Policy = "Owner")]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Address,Phone")] Center input)
    {
        var center = await _db.Centers.FindAsync(id);
        if (center == null) return NotFound();
        if (await _db.Centers.AnyAsync(c => c.Id != id && c.Name == input.Name.Trim()))
            ModelState.AddModelError(nameof(input.Name), "Bu nomdagi markaz allaqachon bor");
        if (!ModelState.IsValid)
        {
            input.Id = id;
            return View(input);
        }

        center.Name = input.Name.Trim();
        center.Address = input.Address;
        center.Phone = input.Phone;
        _audit.Log("Markazni tahrirladi", center.Name, center.Id);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Markaz ma'lumotlari saqlandi";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = "Owner")]
    public async Task<IActionResult> Delete(int id)
    {
        var center = await _db.CenterSummaries().FirstOrDefaultAsync(c => c.Id == id);
        return center == null ? NotFound() : View(center);
    }

    [Authorize(Policy = "Owner")]
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, string? confirmName)
    {
        var center = await _db.Centers.FindAsync(id);
        if (center == null) return NotFound();

        if (!string.Equals(confirmName?.Trim(), center.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Err"] = "Tasdiqlash uchun markaz nomini to'g'ri yozing";
            return RedirectToAction(nameof(Delete), new { id });
        }

        var photos = await _db.Students.Where(s => s.CenterId == id && s.PhotoFileName != null)
            .Select(s => s.PhotoFileName).ToListAsync();
        var studentCount = await _db.Students.CountAsync(s => s.CenterId == id);
        var paid = await _db.Payments.Where(p => p.Student!.CenterId == id).SumAsync(p => (long?)p.Amount) ?? 0;

        // O'quvchilar va to'lovlar bazada cascade bilan o'chadi
        _db.Centers.Remove(center);
        _audit.Log("Markazni o'chirdi", $"{center.Name}: {studentCount} o'quvchi, to'lovlar {Fmt.Money(paid)}");
        await _db.SaveChangesAsync();
        foreach (var p in photos) _photos.Delete(p);

        TempData["Ok"] = $"\"{center.Name}\" markazi o'chirildi";
        return RedirectToAction(nameof(Index));
    }
}
