using PravaMarkaz.Data;
using PravaMarkaz.Models;

namespace PravaMarkaz.Services;

/// <summary>Amallar tarixiga yozuv qo'shadi. Yozuv asosiy o'zgarish bilan birga SaveChanges da saqlanadi.</summary>
public class Audit
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _user;

    public Audit(AppDbContext db, CurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public void Log(string action, string? details = null, int? centerId = null, string? userName = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            At = DateTime.Now,
            UserName = userName ?? _user.UserName,
            Action = action,
            Details = details is { Length: > 1000 } ? details[..1000] : details,
            CenterId = centerId,
            Ip = _user.Ip
        });
    }
}
