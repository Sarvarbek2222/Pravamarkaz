using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using PravaMarkaz.Models;

namespace PravaMarkaz.Services;

/// <summary>Pul, sana va ro'yxat qiymatlarini bir xil ko'rinishda chiqarish.</summary>
public static class Fmt
{
    private static readonly NumberFormatInfo MoneyFormat = new() { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };

    public static readonly string[] Months =
        { "Yanvar", "Fevral", "Mart", "Aprel", "May", "Iyun", "Iyul", "Avgust", "Sentabr", "Oktabr", "Noyabr", "Dekabr" };

    public static readonly string[] Categories = { "A", "B", "BC", "C", "CE", "D", "DE", "BE" };

    public static string Num(long value) => value.ToString("#,0", MoneyFormat);

    public static string Money(long value) => Num(value) + " so'm";

    /// <summary>Katta summani qisqa ko'rsatish: 12,5 mln.</summary>
    public static string Short(long value) => Math.Abs(value) switch
    {
        >= 1_000_000_000 => (value / 1_000_000_000d).ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',') + " mlrd",
        >= 1_000_000 => (value / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',') + " mln",
        _ => Num(value)
    };

    public static string Date(DateTime? value) => value?.ToString("dd.MM.yyyy") ?? "—";

    public static string DateTime(DateTime? value) => value?.ToString("dd.MM.yyyy HH:mm") ?? "—";

    public static string MonthYear(int year, int month) => $"{Months[month - 1]} {year}";

    public static int Percent(long part, long total) =>
        total <= 0 ? (part > 0 ? 100 : 0) : (int)Math.Clamp(Math.Round(part * 100.0 / total), 0, 100);

    public static int Age(DateTime? birth)
    {
        if (birth == null) return 0;
        var today = System.DateTime.Today;
        var age = today.Year - birth.Value.Year;
        return birth.Value.Date > today.AddYears(-age) ? age - 1 : age;
    }

    public static string Label<T>(T value) where T : struct, Enum =>
        typeof(T).GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();

    public static string StatusClass(StudentStatus s) => s switch
    {
        StudentStatus.Studying => "soft-primary",
        StudentStatus.Graduated => "soft-success",
        _ => "soft-secondary"
    };

    public static string MethodIcon(PaymentMethod m) => m switch
    {
        PaymentMethod.Cash => "bi-cash-stack",
        PaymentMethod.Card => "bi-credit-card",
        PaymentMethod.Transfer => "bi-bank",
        _ => "bi-phone"
    };

    public static string Initials(string? last, string? first) =>
        $"{(string.IsNullOrEmpty(last) ? "" : last[..1])}{(string.IsNullOrEmpty(first) ? "" : first[..1])}".ToUpperInvariant();

    // ---- Summani so'z bilan yozish (kvitansiya uchun) ----

    private static readonly string[] Ones = { "", "bir", "ikki", "uch", "to'rt", "besh", "olti", "yetti", "sakkiz", "to'qqiz" };
    private static readonly string[] Tens = { "", "o'n", "yigirma", "o'ttiz", "qirq", "ellik", "oltmish", "yetmish", "sakson", "to'qson" };
    private static readonly string[] Scales = { "", "ming", "million", "milliard", "trillion" };

    public static string Words(long value)
    {
        if (value == 0) return "nol so'm";
        var parts = new List<string>();
        var n = Math.Abs(value);
        var scale = 0;
        while (n > 0)
        {
            var chunk = (int)(n % 1000);
            if (chunk > 0)
            {
                var words = ThreeDigits(chunk);
                parts.Insert(0, Scales[scale].Length > 0 ? $"{words} {Scales[scale]}" : words);
            }
            n /= 1000;
            scale++;
        }
        var text = string.Join(" ", parts) + " so'm";
        return char.ToUpper(text[0]) + text[1..];
    }

    private static string ThreeDigits(int n)
    {
        var words = new List<string>();
        if (n >= 100)
        {
            words.Add(Ones[n / 100]);
            words.Add("yuz");
        }
        if (n % 100 >= 10) words.Add(Tens[n % 100 / 10]);
        if (n % 10 > 0) words.Add(Ones[n % 10]);
        return string.Join(" ", words);
    }
}
