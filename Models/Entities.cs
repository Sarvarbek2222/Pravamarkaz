using System.ComponentModel.DataAnnotations;

namespace PravaMarkaz.Models;

public enum UserRole
{
    [Display(Name = "Egasi")] Owner = 0,
    [Display(Name = "Markaz menejeri")] Manager = 1
}

public enum StudentStatus
{
    [Display(Name = "O'qimoqda")] Studying = 0,
    [Display(Name = "Bitirgan")] Graduated = 1,
    [Display(Name = "Chiqib ketgan")] Dropped = 2
}

public enum PaymentMethod
{
    [Display(Name = "Naqd")] Cash = 0,
    [Display(Name = "Plastik karta")] Card = 1,
    [Display(Name = "Bank o'tkazmasi")] Transfer = 2,
    [Display(Name = "Click / Payme")] Online = 3
}

/// <summary>O'quv markazi (filial).</summary>
public class Center
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Markaz nomini kiriting")]
    [StringLength(150)]
    [Display(Name = "Markaz nomi")]
    public string Name { get; set; } = "";

    [StringLength(300)]
    [Display(Name = "Manzil")]
    public string? Address { get; set; }

    [StringLength(50)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Student> Students { get; set; } = new();
}

/// <summary>Markazga kelgan o'quvchi.</summary>
public class Student
{
    public int Id { get; set; }

    [Display(Name = "Markaz")]
    [Range(1, int.MaxValue, ErrorMessage = "Markazni tanlang")]
    public int CenterId { get; set; }
    public Center? Center { get; set; }

    [Required(ErrorMessage = "Familiyani kiriting")]
    [StringLength(100)]
    [Display(Name = "Familiya")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Ismni kiriting")]
    [StringLength(100)]
    [Display(Name = "Ism")]
    public string FirstName { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "Otasining ismi")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Tug'ilgan sanani kiriting")]
    [DataType(DataType.Date)]
    [Display(Name = "Tug'ilgan sana")]
    public DateTime? BirthDate { get; set; }

    [StringLength(50)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [StringLength(20)]
    [Display(Name = "Pasport seriya va raqami")]
    public string? PassportNo { get; set; }

    [StringLength(300)]
    [Display(Name = "Yashash manzili")]
    public string? Address { get; set; }

    [StringLength(10)]
    [Display(Name = "Toifa")]
    public string? Category { get; set; }

    [StringLength(30)]
    [Display(Name = "Shartnoma raqami")]
    public string? ContractNo { get; set; }

    [Display(Name = "Holati")]
    public StudentStatus Status { get; set; } = StudentStatus.Studying;

    [Range(0, long.MaxValue, ErrorMessage = "Summa manfiy bo'lishi mumkin emas")]
    [Display(Name = "Kurs narxi (so'm)")]
    public long TotalFee { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Qabul qilingan sana")]
    public DateTime EnrolledAt { get; set; } = DateTime.Today;

    [StringLength(500)]
    [Display(Name = "Izoh")]
    public string? Note { get; set; }

    [StringLength(100)]
    public string? PhotoFileName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Payment> Payments { get; set; } = new();

    public string FullName => string.Join(" ", new[] { LastName, FirstName, MiddleName }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

/// <summary>O'quvchi qilgan bitta to'lov.</summary>
public class Payment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Summa 0 dan katta bo'lishi kerak")]
    [Display(Name = "Summa (so'm)")]
    public long Amount { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "To'lov sanasi")]
    public DateTime PaidAt { get; set; } = DateTime.Today;

    [Display(Name = "To'lov usuli")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    [StringLength(300)]
    [Display(Name = "Izoh")]
    public string? Note { get; set; }

    /// <summary>To'lovni qabul qilgan xodim.</summary>
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string ReceiptNo => Id.ToString("D6");
}

/// <summary>Tizimga kiruvchi foydalanuvchi: egasi yoki markaz menejeri.</summary>
public class AppUser
{
    public int Id { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = "";

    [StringLength(100)]
    public string FullName { get; set; } = "";

    [StringLength(200)]
    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Owner;

    /// <summary>Menejer biriktirilgan markaz (egasi uchun null).</summary>
    public int? CenterId { get; set; }
    public Center? Center { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Parol, rol yoki markaz o'zgarganda yangilanadi — eski sessiyalar bekor bo'ladi.</summary>
    [StringLength(40)]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Kim, qachon, nima qilgani.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime At { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string UserName { get; set; } = "";

    [StringLength(100)]
    public string Action { get; set; } = "";

    [StringLength(1000)]
    public string? Details { get; set; }

    public int? CenterId { get; set; }

    [StringLength(50)]
    public string? Ip { get; set; }
}
