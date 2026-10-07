using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Data;
using PravaMarkaz.Models;
using PravaMarkaz.Services;

namespace PravaMarkaz.Controllers;

public class StudentsController : Controller
{
    private const string Fields = "CenterId,LastName,FirstName,MiddleName,BirthDate,Phone,PassportNo,Address,Category,ContractNo,Status,TotalFee,EnrolledAt,Note";

    private readonly AppDbContext _db;
    private readonly PhotoStorage _photos;
    private readonly Audit _audit;
    private readonly CurrentUser _me;

    public StudentsController(AppDbContext db, PhotoStorage photos, Audit audit, CurrentUser me)
    {
        _db = db;
        _photos = photos;
        _audit = audit;
        _me = me;
    }

    public async Task<IActionResult> Index(StudentListFilter filter)
    {
        await LoadCentersAsync(filter.CenterId);
        return View(await _db.StudentListAsync(filter));
    }

    public async Task<IActionResult> Export(StudentListFilter filter)
    {
        var rows = await _db.FilterStudents(filter).SortStudents(filter.Sort).ToRows().ToListAsync();
        var centerName = filter.CenterId is > 0 ? await _db.Centers.Where(c => c.Id == filter.CenterId).Select(c => c.Name).FirstOrDefaultAsync() : null;
        var title = "O'quvchilar ro'yxati" + (centerName != null ? $" — {centerName}" : "");
        var file = ExcelExport.Students(rows, title);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"oquvchilar_{DateTime.Now:yyyy-MM-dd}.xlsx");
    }

    public async Task<IActionResult> Details(int id)
    {
        var student = await _db.Students.Include(s => s.Center).FirstOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound();

        var vm = new StudentDetailsViewModel
        {
            Student = student,
            Payments = await _db.Payments.Where(p => p.StudentId == id)
                .OrderBy(p => p.PaidAt).ThenBy(p => p.Id).ToListAsync()
        };
        vm.NewPayment = new Payment { StudentId = id, PaidAt = DateTime.Today, Amount = Math.Max(0, vm.Debt) };
        return View(vm);
    }

    public async Task<IActionResult> Create(int? centerId)
    {
        if (!await _db.Centers.AnyAsync())
        {
            TempData["Err"] = "Avval o'quv markazini qo'shing";
            return _me.IsOwner ? RedirectToAction("Create", "Centers") : RedirectToAction("Index", "Home");
        }
        var student = new Student { CenterId = _me.IsOwner ? centerId ?? 0 : _me.CenterId ?? 0, EnrolledAt = DateTime.Today, Category = "B" };
        await LoadCentersAsync(student.CenterId);
        return View("Edit", student);
    }

    [HttpPost]
    public async Task<IActionResult> Create([Bind(Fields)] Student student, IFormFile? photo, long firstPayment = 0, PaymentMethod firstPaymentMethod = PaymentMethod.Cash)
    {
        if (!_me.IsOwner) student.CenterId = _me.CenterId ?? 0;

        ValidatePhoto(photo);
        if (firstPayment < 0) ModelState.AddModelError("firstPayment", "Summa manfiy bo'lishi mumkin emas");
        else if (firstPayment > student.TotalFee) ModelState.AddModelError("firstPayment", "Birinchi to'lov kurs narxidan oshmasligi kerak");
        if (!await _db.Centers.AnyAsync(c => c.Id == student.CenterId)) ModelState.AddModelError(nameof(student.CenterId), "Markazni tanlang");
        await CheckDuplicateAsync(student, 0);

        if (!ModelState.IsValid)
        {
            ViewBag.FirstPayment = firstPayment;
            ViewBag.FirstPaymentMethod = firstPaymentMethod;
            await LoadCentersAsync(student.CenterId);
            return View("Edit", student);
        }

        Normalize(student);
        student.CreatedAt = DateTime.Now;
        if (photo is { Length: > 0 }) student.PhotoFileName = await _photos.SaveAsync(photo);
        if (firstPayment > 0)
        {
            student.Payments.Add(new Payment
            {
                Amount = firstPayment, PaidAt = student.EnrolledAt, Method = firstPaymentMethod,
                Note = "Birinchi to'lov", CreatedBy = _me.UserName, CreatedAt = DateTime.Now
            });
        }

        _db.Students.Add(student);
        await _db.SaveChangesAsync();
        _audit.Log("O'quvchi qo'shdi", $"{student.FullName}, kurs narxi {Fmt.Money(student.TotalFee)}" +
            (firstPayment > 0 ? $", birinchi to'lov {Fmt.Money(firstPayment)}" : ""), student.CenterId);
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"{student.FullName} qo'shildi";
        if (firstPayment > 0) TempData["ReceiptId"] = student.Payments[0].Id;
        return RedirectToAction(nameof(Details), new { id = student.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return NotFound();
        await LoadCentersAsync(student.CenterId);
        return View(student);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind(Fields)] Student input, IFormFile? photo, bool removePhoto = false)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return NotFound();
        if (!_me.IsOwner) input.CenterId = student.CenterId;

        ValidatePhoto(photo);
        var paid = await _db.PaidByStudentAsync(id);
        if (input.TotalFee < paid)
            ModelState.AddModelError(nameof(input.TotalFee), $"Kurs narxi allaqachon to'langan {Fmt.Money(paid)} dan kam bo'lishi mumkin emas");
        if (!await _db.Centers.AnyAsync(c => c.Id == input.CenterId)) ModelState.AddModelError(nameof(input.CenterId), "Markazni tanlang");
        await CheckDuplicateAsync(input, id);

        if (!ModelState.IsValid)
        {
            input.Id = id;
            input.PhotoFileName = student.PhotoFileName;
            await LoadCentersAsync(input.CenterId);
            return View(input);
        }

        Normalize(input);
        var changes = new List<string>();
        if (student.TotalFee != input.TotalFee) changes.Add($"kurs narxi {Fmt.Money(student.TotalFee)} → {Fmt.Money(input.TotalFee)}");
        if (student.CenterId != input.CenterId) changes.Add("markaz o'zgardi");
        if (student.Status != input.Status) changes.Add($"holati: {Fmt.Label(input.Status)}");

        student.CenterId = input.CenterId;
        student.LastName = input.LastName;
        student.FirstName = input.FirstName;
        student.MiddleName = input.MiddleName;
        student.BirthDate = input.BirthDate;
        student.Phone = input.Phone;
        student.PassportNo = input.PassportNo;
        student.Address = input.Address;
        student.Category = input.Category;
        student.ContractNo = input.ContractNo;
        student.Status = input.Status;
        student.TotalFee = input.TotalFee;
        student.EnrolledAt = input.EnrolledAt;
        student.Note = input.Note;

        string? oldPhoto = null;
        if (photo is { Length: > 0 })
        {
            oldPhoto = student.PhotoFileName;
            student.PhotoFileName = await _photos.SaveAsync(photo);
        }
        else if (removePhoto)
        {
            oldPhoto = student.PhotoFileName;
            student.PhotoFileName = null;
        }

        _audit.Log("O'quvchini tahrirladi", student.FullName + (changes.Count > 0 ? ": " + string.Join(", ", changes) : ""), student.CenterId);
        await _db.SaveChangesAsync();
        _photos.Delete(oldPhoto);
        TempData["Ok"] = "O'quvchi ma'lumotlari saqlandi";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = "Owner")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _db.Students.Include(s => s.Center).Include(s => s.Payments).FirstOrDefaultAsync(s => s.Id == id);
        return student == null ? NotFound() : View(student);
    }

    [Authorize(Policy = "Owner")]
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return NotFound();

        var paid = await _db.PaidByStudentAsync(id);
        _db.Students.Remove(student);
        _audit.Log("O'quvchini o'chirdi", $"{student.FullName}, to'lovlari {Fmt.Money(paid)}", student.CenterId);
        await _db.SaveChangesAsync();
        _photos.Delete(student.PhotoFileName);

        TempData["Ok"] = $"{student.FullName} o'chirildi";
        return RedirectToAction("Details", "Centers", new { id = student.CenterId });
    }

    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Photo(int id)
    {
        var name = await _db.Students.Where(s => s.Id == id).Select(s => s.PhotoFileName).FirstOrDefaultAsync();
        var path = _photos.GetPath(name);
        if (path == null || !System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, PhotoStorage.ContentType(path));
    }

    private async Task CheckDuplicateAsync(Student s, int exceptId)
    {
        if (!string.IsNullOrWhiteSpace(s.PassportNo))
        {
            var passport = s.PassportNo.Replace(" ", "").ToUpperInvariant();
            var other = await _db.Students.IgnoreQueryFilters()
                .Where(x => x.Id != exceptId && x.PassportNo == passport)
                .Select(x => x.LastName + " " + x.FirstName).FirstOrDefaultAsync();
            if (other != null) ModelState.AddModelError(nameof(s.PassportNo), $"Bu pasport bilan o'quvchi bor: {other}");
        }
    }

    private void ValidatePhoto(IFormFile? photo)
    {
        var error = PhotoStorage.Validate(photo);
        if (error != null) ModelState.AddModelError("photo", error);
    }

    private static void Normalize(Student s)
    {
        static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        s.LastName = s.LastName.Trim();
        s.FirstName = s.FirstName.Trim();
        s.MiddleName = Clean(s.MiddleName);
        s.Phone = Clean(s.Phone);
        s.PassportNo = Clean(s.PassportNo)?.Replace(" ", "").ToUpperInvariant();
        s.Address = Clean(s.Address);
        s.Category = Clean(s.Category);
        s.ContractNo = Clean(s.ContractNo);
        s.Note = Clean(s.Note);
    }

    private async Task LoadCentersAsync(int? selected)
    {
        var centers = await _db.Centers.OrderBy(c => c.Name).Select(c => new { c.Id, c.Name }).ToListAsync();
        ViewBag.Centers = new SelectList(centers, "Id", "Name", selected);
    }
}
