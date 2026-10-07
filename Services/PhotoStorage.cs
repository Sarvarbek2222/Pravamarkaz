namespace PravaMarkaz.Services;

/// <summary>
/// O'quvchi rasmlarini wwwroot dan tashqarida saqlaydi —
/// rasmlarni faqat tizimga kirgan foydalanuvchi ko'ra oladi.
/// </summary>
public class PhotoStorage
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private readonly string _dir;

    public PhotoStorage(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(_dir);
    }

    /// <summary>Faylni tekshiradi. Xato bo'lsa xabar matnini qaytaradi, aks holda null.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > MaxBytes) return "Rasm hajmi 5 MB dan oshmasligi kerak";
        if (!Allowed.ContainsKey(Path.GetExtension(file.FileName))) return "Faqat JPG, PNG yoki WEBP rasm yuklang";
        return null;
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        var name = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = File.Create(Path.Combine(_dir, name));
        await file.CopyToAsync(stream);
        return name;
    }

    public void Delete(string? name)
    {
        var path = GetPath(name);
        if (path != null && File.Exists(path)) File.Delete(path);
    }

    public string? GetPath(string? name)
    {
        if (string.IsNullOrEmpty(name) || name != Path.GetFileName(name)) return null;
        return Path.Combine(_dir, name);
    }

    public static string ContentType(string name) =>
        Allowed.TryGetValue(Path.GetExtension(name), out var type) ? type : "application/octet-stream";
}
