using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Data;
using PravaMarkaz.Models;
using PravaMarkaz.Services;

namespace PravaMarkaz.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var monthStart = Queries.MonthStart();
        var lastMonthStart = monthStart.AddMonths(-1);
        var chartStart = monthStart.AddMonths(-11);

        var vm = new DashboardViewModel
        {
            Centers = await _db.CenterSummaries().OrderBy(c => c.Name).ToListAsync(),
            RecentPayments = await _db.Payments
                .OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id)
                .Take(8)
                .PaymentRows()
                .ToListAsync(),
            TopDebtors = await _db.FilterStudents(new StudentListFilter { Pay = "debt" })
                .SortStudents("debt")
                .Take(8)
                .ToRows()
                .ToListAsync(),
            Monthly = await _db.Payments
                .Where(p => p.PaidAt >= chartStart)
                .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month, p.Student!.CenterId })
                .Select(g => new MonthPoint { Year = g.Key.Year, Month = g.Key.Month, CenterId = g.Key.CenterId, Sum = g.Sum(p => p.Amount) })
                .ToListAsync(),
            PaidToday = await _db.Payments.Where(p => p.PaidAt >= today).SumAsync(p => (long?)p.Amount) ?? 0,
            PaidThisMonth = await _db.Payments.Where(p => p.PaidAt >= monthStart).SumAsync(p => (long?)p.Amount) ?? 0,
            PaidLastMonth = await _db.Payments.Where(p => p.PaidAt >= lastMonthStart && p.PaidAt < monthStart).SumAsync(p => (long?)p.Amount) ?? 0,
            NewThisMonth = await _db.Students.CountAsync(s => s.EnrolledAt >= monthStart)
        };
        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
