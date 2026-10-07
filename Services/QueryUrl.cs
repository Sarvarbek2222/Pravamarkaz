using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;

namespace PravaMarkaz.Services;

/// <summary>Joriy so'rov manzilini bitta parametrni almashtirib qaytaradi (sahifalash, saralash uchun).</summary>
public static class QueryUrl
{
    public static string With(HttpRequest request, string key, string? value, bool resetPage = true)
    {
        var dict = request.Query.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(value)) dict.Remove(key);
        else dict[key] = new StringValues(value);
        if (resetPage && !key.Equals("page", StringComparison.OrdinalIgnoreCase)) dict.Remove("page");

        var qb = new QueryBuilder();
        foreach (var (k, v) in dict)
            foreach (var item in v)
                if (!string.IsNullOrEmpty(item)) qb.Add(k, item);
        return request.PathBase + request.Path + qb.ToQueryString();
    }

    /// <summary>Excel eksport kabi boshqa amal uchun joriy filtrlar bilan manzil.</summary>
    public static string ForAction(HttpRequest request, string path)
    {
        var qb = new QueryBuilder();
        foreach (var (k, v) in request.Query)
            if (!k.Equals("page", StringComparison.OrdinalIgnoreCase))
                foreach (var item in v)
                    if (!string.IsNullOrEmpty(item)) qb.Add(k, item);
        return request.PathBase + path + qb.ToQueryString();
    }
}
