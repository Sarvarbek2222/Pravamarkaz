using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Data;
using PravaMarkaz.Models;

namespace PravaMarkaz.Services;

/// <summary>Hisob-kitob uchun umumiy so'rovlar. Menejerlar uchun markaz cheklovi AppDbContext filtrida.</summary>
public static class Queries
{
    public const int PageSize = 50;

    public static DateTime MonthStart(DateTime? date = null)
    {
        var d = date ?? DateTime.Today;
        return new DateTime(d.Year, d.Month, 1);
    }

    public static IQueryable<CenterSummary> CenterSummaries(this AppDbContext db)
    {
        var monthStart = MonthStart();
        return db.Centers.Select(c => new CenterSummary
        {
            Id = c.Id,
            Name = c.Name,
            Address = c.Address,
            Phone = c.Phone,
            StudentCount = c.Students.Count(),
            ActiveCount = c.Students.Count(s => s.Status == StudentStatus.Studying),
            TotalFee = c.Students.Sum(s => (long?)s.TotalFee) ?? 0,
            Paid = c.Students.SelectMany(s => s.Payments).Sum(p => (long?)p.Amount) ?? 0,
            PaidThisMonth = c.Students.SelectMany(s => s.Payments).Where(p => p.PaidAt >= monthStart).Sum(p => (long?)p.Amount) ?? 0,
            DebtorCount = c.Students.Count(s => s.TotalFee > (s.Payments.Sum(p => (long?)p.Amount) ?? 0))
        });
    }

    public static IQueryable<Student> FilterStudents(this AppDbContext db, StudentListFilter f)
    {
        var q = db.Students.AsQueryable();

        if (f.CenterId is > 0) q = q.Where(s => s.CenterId == f.CenterId);
        if (f.Status != null) q = q.Where(s => s.Status == f.Status);
        if (!string.IsNullOrEmpty(f.Category)) q = q.Where(s => s.Category == f.Category);

        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            foreach (var term in f.Q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                q = q.Where(s => s.LastName.Contains(term) || s.FirstName.Contains(term)
                    || (s.MiddleName != null && s.MiddleName.Contains(term))
                    || (s.Phone != null && s.Phone.Contains(term))
                    || (s.PassportNo != null && s.PassportNo.Contains(term))
                    || (s.ContractNo != null && s.ContractNo.Contains(term)));
            }
        }

        if (f.Pay == "debt")
            q = q.Where(s => s.TotalFee > (s.Payments.Sum(p => (long?)p.Amount) ?? 0));
        else if (f.Pay == "paid")
            q = q.Where(s => s.TotalFee <= (s.Payments.Sum(p => (long?)p.Amount) ?? 0));

        return q;
    }

    public static IQueryable<Student> SortStudents(this IQueryable<Student> q, string? sort) => sort switch
    {
        "debt" => q.OrderByDescending(s => s.TotalFee - (s.Payments.Sum(p => (long?)p.Amount) ?? 0)).ThenBy(s => s.LastName),
        "paid" => q.OrderByDescending(s => s.Payments.Sum(p => (long?)p.Amount) ?? 0).ThenBy(s => s.LastName),
        "enrolled" => q.OrderByDescending(s => s.EnrolledAt).ThenByDescending(s => s.Id),
        "lastpay" => q.OrderByDescending(s => s.Payments.Max(p => (DateTime?)p.PaidAt)).ThenBy(s => s.LastName),
        _ => q.OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
    };

    public static IQueryable<StudentRow> ToRows(this IQueryable<Student> q) =>
        q.Select(s => new StudentRow
        {
            Id = s.Id,
            CenterId = s.CenterId,
            CenterName = s.Center!.Name,
            LastName = s.LastName,
            FirstName = s.FirstName,
            MiddleName = s.MiddleName,
            BirthDate = s.BirthDate,
            Phone = s.Phone,
            Category = s.Category,
            Status = s.Status,
            HasPhoto = s.PhotoFileName != null,
            EnrolledAt = s.EnrolledAt,
            TotalFee = s.TotalFee,
            Paid = s.Payments.Sum(p => (long?)p.Amount) ?? 0,
            LastPaidAt = s.Payments.Max(p => (DateTime?)p.PaidAt),
            PaymentCount = s.Payments.Count()
        });

    public static async Task<StudentListViewModel> StudentListAsync(this AppDbContext db, StudentListFilter f)
    {
        var q = db.FilterStudents(f);
        var ids = q.Select(s => s.Id);

        var total = await q.CountAsync();
        var page = Math.Clamp(f.Page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)));
        f.Page = page;

        return new StudentListViewModel
        {
            Filter = f,
            Summary = new MoneySummary
            {
                StudentCount = total,
                TotalFee = await q.SumAsync(s => (long?)s.TotalFee) ?? 0,
                Paid = await db.Payments.Where(p => ids.Contains(p.StudentId)).SumAsync(p => (long?)p.Amount) ?? 0
            },
            DebtorCount = await q.CountAsync(s => s.TotalFee > (s.Payments.Sum(p => (long?)p.Amount) ?? 0)),
            Rows = new PagedList<StudentRow>
            {
                Page = page,
                PageSize = PageSize,
                Total = total,
                Items = await q.SortStudents(f.Sort).Skip((page - 1) * PageSize).Take(PageSize).ToRows().ToListAsync()
            }
        };
    }

    public static IQueryable<PaymentRow> PaymentRows(this IQueryable<Payment> q) =>
        q.Select(p => new PaymentRow
        {
            Id = p.Id,
            StudentId = p.StudentId,
            StudentName = p.Student!.LastName + " " + p.Student.FirstName,
            CenterId = p.Student.CenterId,
            CenterName = p.Student.Center!.Name,
            Amount = p.Amount,
            PaidAt = p.PaidAt,
            Method = p.Method,
            Note = p.Note,
            CreatedBy = p.CreatedBy
        });

    public static async Task<long> PaidByStudentAsync(this AppDbContext db, int studentId) =>
        await db.Payments.Where(p => p.StudentId == studentId).SumAsync(p => (long?)p.Amount) ?? 0;
}
