using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Models;
using PravaMarkaz.Services;

namespace PravaMarkaz.Data;

public class AppDbContext : DbContext
{
    // Menejer faqat o'z markazini ko'rishi uchun global filtr qiymati (egasi uchun null).
    // So'rovdan tashqarida (ilova ishga tushishi, migratsiya) filtr yo'q.
    private readonly int? _scopeCenterId;

    public AppDbContext(DbContextOptions<AppDbContext> options, CurrentUser? user = null) : base(options)
    {
        _scopeCenterId = user is { HasHttpContext: true } ? user.ScopeCenterId : null;
    }

    public DbSet<Center> Centers => Set<Center>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Center>()
            .HasMany(c => c.Students)
            .WithOne(s => s.Center!)
            .HasForeignKey(s => s.CenterId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Student>()
            .HasMany(s => s.Payments)
            .WithOne(p => p.Student!)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<AppUser>()
            .HasOne(u => u.Center)
            .WithMany()
            .HasForeignKey(u => u.CenterId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<Student>().Ignore(s => s.FullName);
        b.Entity<Payment>().Ignore(p => p.ReceiptNo);

        b.Entity<Student>().HasIndex(s => new { s.CenterId, s.LastName });
        b.Entity<Student>().HasIndex(s => s.Status);
        b.Entity<Payment>().HasIndex(p => p.PaidAt);
        b.Entity<AppUser>().HasIndex(u => u.Username).IsUnique();
        b.Entity<AuditLog>().HasIndex(a => a.At);

        b.Entity<Center>().HasQueryFilter(c => _scopeCenterId == null || c.Id == _scopeCenterId);
        b.Entity<Student>().HasQueryFilter(s => _scopeCenterId == null || s.CenterId == _scopeCenterId);
        b.Entity<Payment>().HasQueryFilter(p => _scopeCenterId == null || p.Student!.CenterId == _scopeCenterId);
    }
}
