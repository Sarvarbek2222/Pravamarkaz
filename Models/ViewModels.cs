using System.ComponentModel.DataAnnotations;

namespace PravaMarkaz.Models;

public class MoneySummary
{
    public int StudentCount { get; set; }
    public long TotalFee { get; set; }
    public long Paid { get; set; }
    public long Debt => TotalFee - Paid;
}

public class CenterSummary : MoneySummary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public int ActiveCount { get; set; }
    public int DebtorCount { get; set; }
    public long PaidThisMonth { get; set; }
}

public class StudentRow
{
    public int Id { get; set; }
    public int CenterId { get; set; }
    public string CenterName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string FullName => string.Join(" ", new[] { LastName, FirstName, MiddleName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public DateTime? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Category { get; set; }
    public StudentStatus Status { get; set; }
    public bool HasPhoto { get; set; }
    public DateTime EnrolledAt { get; set; }
    public long TotalFee { get; set; }
    public long Paid { get; set; }
    public long Debt => TotalFee - Paid;
    public DateTime? LastPaidAt { get; set; }
    public int PaymentCount { get; set; }
}

public class StudentListFilter
{
    public int? CenterId { get; set; }
    public string? Q { get; set; }
    /// <summary>"debt" — qarzdorlar, "paid" — to'liq to'laganlar.</summary>
    public string? Pay { get; set; }
    public StudentStatus? Status { get; set; }
    public string? Category { get; set; }
    /// <summary>name | debt | paid | enrolled | lastpay</summary>
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
}

public class PagedList<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int Total { get; set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    public int FirstIndex => (Page - 1) * PageSize;
    public PagerInfo Pager => new() { Page = Page, PageCount = PageCount, Total = Total, From = FirstIndex + 1, To = FirstIndex + Items.Count };
}

public class PagerInfo
{
    public int Page { get; set; }
    public int PageCount { get; set; }
    public int Total { get; set; }
    public int From { get; set; }
    public int To { get; set; }
}

public class StudentListViewModel
{
    public StudentListFilter Filter { get; set; } = new();
    public PagedList<StudentRow> Rows { get; set; } = new();
    public MoneySummary Summary { get; set; } = new();
    public int DebtorCount { get; set; }
    public bool ShowCenterColumn { get; set; } = true;
}

public class MonthPoint
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int CenterId { get; set; }
    public long Sum { get; set; }
}

public class DashboardViewModel
{
    public List<CenterSummary> Centers { get; set; } = new();
    public List<PaymentRow> RecentPayments { get; set; } = new();
    public List<StudentRow> TopDebtors { get; set; } = new();
    public List<MonthPoint> Monthly { get; set; } = new();
    public long PaidToday { get; set; }
    public long PaidThisMonth { get; set; }
    public long PaidLastMonth { get; set; }
    public int NewThisMonth { get; set; }

    public MoneySummary Total => new()
    {
        StudentCount = Centers.Sum(c => c.StudentCount),
        TotalFee = Centers.Sum(c => c.TotalFee),
        Paid = Centers.Sum(c => c.Paid)
    };
}

public class CenterDetailsViewModel
{
    public CenterSummary Center { get; set; } = new();
    public StudentListViewModel Students { get; set; } = new();
}

public class StudentDetailsViewModel
{
    public Student Student { get; set; } = null!;
    public List<Payment> Payments { get; set; } = new();
    public Payment NewPayment { get; set; } = new();
    public long Paid => Payments.Sum(p => p.Amount);
    public long Debt => Student.TotalFee - Paid;
}

public class PaymentRow
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public int CenterId { get; set; }
    public string CenterName { get; set; } = "";
    public long Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
}

public class GroupTotal
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public long Sum { get; set; }
}

public class PaymentListViewModel
{
    public int? CenterId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public PaymentMethod? Method { get; set; }
    public PagedList<PaymentRow> Rows { get; set; } = new();
    public long Sum { get; set; }
    public List<GroupTotal> ByCenter { get; set; } = new();
    public List<GroupTotal> ByMethod { get; set; } = new();
}

public class ReceiptViewModel
{
    public Payment Payment { get; set; } = null!;
    public long PaidTotal { get; set; }
    public long Debt => Payment.Student!.TotalFee - PaidTotal;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Loginni kiriting")]
    [Display(Name = "Login")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Parolni kiriting")]
    [DataType(DataType.Password)]
    [Display(Name = "Parol")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Joriy parolni kiriting")]
    [DataType(DataType.Password)]
    [Display(Name = "Joriy parol")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Yangi parolni kiriting")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Parol kamida 6 ta belgi bo'lsin")]
    [DataType(DataType.Password)]
    [Display(Name = "Yangi parol")]
    public string NewPassword { get; set; } = "";

    [Compare(nameof(NewPassword), ErrorMessage = "Parollar mos emas")]
    [DataType(DataType.Password)]
    [Display(Name = "Yangi parolni takrorlang")]
    public string ConfirmPassword { get; set; } = "";
}

public class UserFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "F.I.Sh. ni kiriting")]
    [StringLength(100)]
    [Display(Name = "F.I.Sh.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Loginni kiriting")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Login 3–50 belgi bo'lsin")]
    [RegularExpression(@"^[a-zA-Z0-9_.\-]+$", ErrorMessage = "Login faqat lotin harflari, raqam va _ . - dan iborat bo'lsin")]
    [Display(Name = "Login")]
    public string Username { get; set; } = "";

    [Display(Name = "Rol")]
    public UserRole Role { get; set; } = UserRole.Manager;

    [Display(Name = "Markaz")]
    public int? CenterId { get; set; }

    [Display(Name = "Faol")]
    public bool IsActive { get; set; } = true;

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Parol kamida 6 ta belgi bo'lsin")]
    [DataType(DataType.Password)]
    [Display(Name = "Parol")]
    public string? Password { get; set; }
}

public class AuditListViewModel
{
    public string? Q { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public PagedList<AuditLog> Rows { get; set; } = new();
}
