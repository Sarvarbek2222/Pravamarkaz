using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Models;

namespace PravaMarkaz.Data;

/// <summary>
/// Sinov uchun namunaviy ma'lumotlar: 3 markaz, har birida 10 o'quvchi, to'lovlar, 2 menejer.
/// Ishga tushirish: dotnet run -- --seed-demo
/// </summary>
public static class DemoSeeder
{
    private static readonly string[] MaleFirst = { "Aziz", "Jasur", "Sardor", "Bekzod", "Otabek", "Sherzod", "Dilshod", "Javohir", "Ulug'bek", "Rustam", "Akmal", "Farrux", "Bobur", "Shoxrux", "Islom" };
    private static readonly string[] FemaleFirst = { "Malika", "Dilnoza", "Nilufar", "Gulnora", "Shahnoza", "Madina", "Zarina", "Sevara", "Kamola", "Feruza" };
    private static readonly string[] LastRoots = { "Karimov", "Toshmatov", "Rahimov", "Yusupov", "Abdullayev", "Nazarov", "Ergashev", "Qodirov", "Saidov", "Xolmatov", "Mirzayev", "Usmonov", "Sobirov", "Hasanov", "Islomov" };
    private static readonly string[] Fathers = { "Rustam", "Anvar", "Baxtiyor", "Shuhrat", "Alisher", "Komil", "Ravshan", "Jamshid", "Ilhom", "Murod" };
    private static readonly string[] Districts = { "Chilonzor tumani", "Yunusobod tumani", "Sergeli tumani", "Mirzo Ulug'bek tumani", "Yakkasaroy tumani", "Olmazor tumani" };

    public static void Seed(AppDbContext db)
    {
        if (db.Centers.Any(c => c.Name.StartsWith("Demo:")))
        {
            Console.WriteLine("Namunaviy ma'lumotlar allaqachon qo'shilgan.");
            return;
        }

        var rnd = new Random(2026);
        var today = DateTime.Today;
        var centers = new[]
        {
            new Center { Name = "Demo: Chilonzor filiali", Address = "Toshkent, Chilonzor 9-kvartal, 12-uy", Phone = "+998 71 200 11 22" },
            new Center { Name = "Demo: Yunusobod filiali", Address = "Toshkent, Yunusobod 4-mavze, 7-uy", Phone = "+998 71 200 33 44" },
            new Center { Name = "Demo: Samarqand filiali", Address = "Samarqand, Rudakiy ko'chasi, 45", Phone = "+998 66 233 55 66" }
        };
        db.Centers.AddRange(centers);
        db.SaveChanges();

        var methods = new[] { PaymentMethod.Cash, PaymentMethod.Cash, PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.Online, PaymentMethod.Transfer };
        var cashiers = new[] { "admin", "menejer1", "menejer2" };
        var contract = 100;

        foreach (var (center, ci) in centers.Select((c, i) => (c, i)))
        {
            for (var n = 0; n < 10; n++)
            {
                var female = rnd.Next(4) == 0;
                var root = LastRoots[rnd.Next(LastRoots.Length)];
                var father = Fathers[rnd.Next(Fathers.Length)];
                var category = rnd.Next(10) switch { < 6 => "B", < 8 => "BC", 8 => "C", _ => "A" };
                var fee = category switch { "BC" => 4_500_000L, "C" => 4_000_000L, "A" => 1_800_000L, _ => 3_500_000L };
                if (rnd.Next(5) == 0) fee -= 300_000; // chegirma

                var enrolled = today.AddDays(-rnd.Next(5, 240));
                var status = (today - enrolled).Days > 150 ? (rnd.Next(4) == 0 ? StudentStatus.Dropped : StudentStatus.Graduated) : StudentStatus.Studying;

                var student = new Student
                {
                    CenterId = center.Id,
                    LastName = female ? root + "a" : root,
                    FirstName = female ? FemaleFirst[rnd.Next(FemaleFirst.Length)] : MaleFirst[rnd.Next(MaleFirst.Length)],
                    MiddleName = father + (female ? " qizi" : "ovich"),
                    BirthDate = today.AddYears(-rnd.Next(18, 45)).AddDays(-rnd.Next(365)),
                    Phone = $"+998 9{rnd.Next(0, 10)} {rnd.Next(100, 999)} {rnd.Next(10, 99)} {rnd.Next(10, 99)}",
                    PassportNo = $"A{(char)('A' + rnd.Next(26))}{rnd.Next(1000000, 9999999)}",
                    Address = ci == 2 ? "Samarqand shahri" : "Toshkent, " + Districts[rnd.Next(Districts.Length)],
                    Category = category,
                    ContractNo = $"2026/{++contract}",
                    Status = status,
                    TotalFee = fee,
                    EnrolledAt = enrolled,
                    CreatedAt = enrolled.AddHours(10)
                };

                // To'lov holati: ~40% to'liq to'lagan, qolganlari qisman, ba'zilari umuman to'lamagan
                var target = rnd.Next(10) switch
                {
                    < 4 => fee,
                    < 8 => fee * rnd.Next(25, 85) / 100 / 50_000 * 50_000,
                    9 => 0,
                    _ => fee / 2 / 50_000 * 50_000
                };
                if (status == StudentStatus.Graduated) target = fee;

                var paid = 0L;
                var date = enrolled;
                var first = true;
                while (paid < target)
                {
                    var amount = first ? Math.Min(target, rnd.Next(2, 5) * 500_000L) : Math.Min(target - paid, rnd.Next(1, 4) * 500_000L);
                    if (date > today) date = today;
                    student.Payments.Add(new Payment
                    {
                        Amount = amount,
                        PaidAt = date,
                        Method = methods[rnd.Next(methods.Length)],
                        Note = first ? "Birinchi to'lov" : null,
                        CreatedBy = cashiers[ci == 0 ? 0 : rnd.Next(2) == 0 ? 0 : ci],
                        CreatedAt = date.AddHours(9 + rnd.Next(9)).AddMinutes(rnd.Next(60))
                    });
                    paid += amount;
                    first = false;
                    date = date.AddDays(rnd.Next(14, 45));
                }

                db.Students.Add(student);
            }
        }
        db.SaveChanges();

        // Menejerlar (parol: menejer123)
        var hasher = new PasswordHasher<AppUser>();
        foreach (var (login, name, center) in new[] { ("menejer1", "Aliyev Vali", centers[1]), ("menejer2", "Karimova Nodira", centers[2]) })
        {
            if (db.Users.Any(u => u.Username == login)) continue;
            var u = new AppUser { Username = login, FullName = name, Role = UserRole.Manager, CenterId = center.Id, IsActive = true };
            u.PasswordHash = hasher.HashPassword(u, "menejer123");
            db.Users.Add(u);
        }

        db.AuditLogs.Add(new AuditLog { UserName = "system", Action = "Namunaviy ma'lumotlar qo'shildi", Details = "3 markaz, 30 o'quvchi, to'lovlar va 2 menejer" });
        db.SaveChanges();

        var students = db.Students.Count(s => s.Center!.Name.StartsWith("Demo:"));
        var payments = db.Payments.Count(p => p.Student!.Center!.Name.StartsWith("Demo:"));
        Console.WriteLine($"Tayyor: 3 markaz, {students} o'quvchi, {payments} to'lov, 2 menejer (parol: menejer123).");
    }
}
