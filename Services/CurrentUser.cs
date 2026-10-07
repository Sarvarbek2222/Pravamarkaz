using System.Security.Claims;
using PravaMarkaz.Models;

namespace PravaMarkaz.Services;

/// <summary>Joriy so'rovdagi foydalanuvchi: kim, qaysi rolda, qaysi markazga biriktirilgan.</summary>
public class CurrentUser
{
    public const string CenterClaim = "center";
    public const string StampClaim = "stamp";
    public const string FullNameClaim = "fullname";

    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    /// <summary>So'rov ichidami (ilova ishga tushishi yoki migratsiya emas).</summary>
    public bool HasHttpContext => _accessor.HttpContext != null;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public int? Id => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string UserName => IsAuthenticated ? Principal!.Identity!.Name ?? "" : "";
    public string FullName => Principal?.FindFirstValue(FullNameClaim) ?? UserName;
    public bool IsOwner => IsAuthenticated && Principal!.IsInRole(nameof(UserRole.Owner));
    public int? CenterId => int.TryParse(Principal?.FindFirstValue(CenterClaim), out var c) ? c : null;
    public string? Ip => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// Ma'lumotlar qaysi markaz bilan cheklanadi: egasi uchun null (hammasi),
    /// menejer uchun o'z markazi, tizimga kirmagan uchun hech narsa (-1).
    /// </summary>
    public int? ScopeCenterId => IsOwner ? null : CenterId ?? -1;
}
