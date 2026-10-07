using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Data;
using PravaMarkaz.Models;
using PravaMarkaz.Services;

namespace PravaMarkaz.Controllers;

public class PaymentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly Audit _audit;
    private readonly CurrentUser _me;

    public PaymentsController(AppDbContext db, Audit audit, CurrentUser me)
    {
        _db = db;
        _audit = audit;
        _me = me;
    }

    private IQueryable<Payment> Filter(int? centerId, DateTime from, DateTime to, PaymentMethod? method)
    {
        var q = _db.Payments.Where(p => p.PaidAt >= from.Date && p.PaidAt < to.Date.AddDays(1));
        if (centerId is > 0) q = q.Where(p => p.Student!.CenterId == centerId);
        if (method != null) q = q.Where(p => p.Method == method);
        return q;
    }

    /// <summary>Tanlangan davrda olingan to'lovlar, markazlar va to'lov usullari bo'yicha jami bilan.</summary>
    public async Task<IActionResult> Index(int? centerId, DateTime? from, DateTime? to, PaymentMethod? method, int page = 1)
    {
        var f = from ?? Queries.MonthStart();
        var t = to ?? DateTime.Today;
        var q = Filter(centerId, f, t, method);

        var total = await q.CountAsync();
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)Queries.PageSize));
        page = Math.Clamp(page, 1, pageCount);

        var byCenter = await q.GroupBy(p => p.Student!.Center!.Name)
            .Select(g => new GroupTotal { Name = g.Key, Count = g.Count(), Sum = g.Sum(p => p.Amount) })
            .OrderByDescending(g => g.Sum).ToListAsync();
        var byMethod = await q.GroupBy(p => p.Method)
            .Select(g => new { g.Key, Count = g.Count(), Sum = g.Sum(p => p.Amount) })
            .ToListAsync();

        var centers = await _db.Centers.OrderBy(c => c.Name).Select(c => new { c.Id, c.Name }).ToListAsync();
        ViewBag.Centers = new SelectList(centers, "Id", "Name", centerId);

        return View(new PaymentListViewModel
        {
            CenterId = centerId,
            From = f,
            To = t,
            Method = method,
            Sum = byCenter.Sum(c => c.Sum),
            ByCenter = byCenter,
            ByMethod = byMethod.OrderByDescending(m => m.Sum)
                .Select(m => new GroupTotal { Name = Fmt.Label(m.Key), Count = m.Count, Sum = m.Sum }).ToList(),
            Rows = new PagedList<PaymentRow>
            {
                Page = page,
                PageSize = Queries.PageSize,
                Total = total,
                Items = await q.OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id)
                    .Skip((page - 1) * Queries.PageSize).Take(Queries.PageSize)
                    .PaymentRows().ToListAsync()
            }
        });
    }

    public async Task<IActionResult> Export(int? centerId, DateTime? from, DateTime? to, PaymentMethod? method)
    {
        var f = from ?? Queries.MonthStart();
        var t = to ?? DateTime.Today;
        var rows = await Filter(centerId, f, t, method)
            .OrderBy(p => p.PaidAt).ThenBy(p => p.Id).PaymentRows().ToListAsync();
        var file = ExcelExport.Payments(rows, $"To'lovlar: {f:dd.MM.yyyy} — {t:dd.MM.yyyy}");
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"tolovlar_{f:yyyy-MM-dd}_{t:yyyy-MM-dd}.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> Create([Bind("StudentId,Amount,PaidAt,Method,Note")] Payment payment)
    {
        var student = await _db.Students.FindAsync(payment.StudentId);
        if (student == null) return NotFound();

        var debt = student.TotalFee - await _db.PaidByStudentAsync(student.Id);
        if (!ModelState.IsValid)
            TempData["Err"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        else if (payment.Amount > debt)
            TempData["Err"] = $"To'lov qolgan qarzdan ({Fmt.Money(debt)}) ko'p bo'lishi mumkin emas";
        else if (payment.PaidAt.Date > DateTime.Today)
            TempData["Err"] = "To'lov sanasi kelajakda bo'lishi mumkin emas";
        else
        {
            payment.Note = string.IsNullOrWhiteSpace(payment.Note) ? null : payment.Note.Trim();
            payment.CreatedBy = _me.UserName;
            payment.CreatedAt = DateTime.Now;
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            _audit.Log("To'lov qabul qildi", $"№{payment.ReceiptNo}, {student.LastName} {student.FirstName}: {Fmt.Money(payment.Amount)} ({Fmt.Label(payment.Method)})", student.CenterId);
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"{Fmt.Money(payment.Amount)} to'lov qabul qilindi";
            TempData["ReceiptId"] = payment.Id;
        }
        return RedirectToAction("Details", "Students", new { id = student.Id });
    }

    /// <summary>Chop etiladigan kvitansiya.</summary>
    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await _db.Payments.Include(p => p.Student).ThenInclude(s => s!.Center).FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();

        // Shu to'lov bilan birga shu paytgacha to'langan summa
        var paidTotal = await _db.Payments
            .Where(p => p.StudentId == payment.StudentId && (p.PaidAt < payment.PaidAt || (p.PaidAt == payment.PaidAt && p.Id <= payment.Id)))
            .SumAsync(p => (long?)p.Amount) ?? 0;
        return View(new ReceiptViewModel { Payment = payment, PaidTotal = paidTotal });
    }

    [Authorize(Policy = "Owner")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var payment = await _db.Payments.Include(p => p.Student).FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();

        _db.Payments.Remove(payment);
        _audit.Log("To'lovni o'chirdi", $"№{payment.ReceiptNo}, {payment.Student!.LastName} {payment.Student.FirstName}: {Fmt.Money(payment.Amount)}, sana {Fmt.Date(payment.PaidAt)}", payment.Student.CenterId);
        await _db.SaveChangesAsync();
        TempData["Ok"] = $"{Fmt.Money(payment.Amount)} lik to'lov o'chirildi";
        return RedirectToAction("Details", "Students", new { id = payment.StudentId });
    }
}
