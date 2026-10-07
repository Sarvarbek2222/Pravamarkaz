using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Data;
using PravaMarkaz.Models;
using PravaMarkaz.Services;

namespace PravaMarkaz.Controllers;

/// <summary>Amallar tarixi — kim, qachon, nima qilgan.</summary>
[Authorize(Policy = "Owner")]
public class AuditController : Controller
{
    private readonly AppDbContext _db;

    public AuditController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, DateTime? from, DateTime? to, int page = 1)
    {
        var query = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => a.UserName.Contains(q) || a.Action.Contains(q) || (a.Details != null && a.Details.Contains(q)));
        if (from != null) query = query.Where(a => a.At >= from.Value.Date);
        if (to != null) query = query.Where(a => a.At < to.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)Queries.PageSize)));

        return View(new AuditListViewModel
        {
            Q = q,
            From = from,
            To = to,
            Rows = new PagedList<AuditLog>
            {
                Page = page,
                PageSize = Queries.PageSize,
                Total = total,
                Items = await query.OrderByDescending(a => a.Id).Skip((page - 1) * Queries.PageSize).Take(Queries.PageSize).ToListAsync()
            }
        });
    }
}
