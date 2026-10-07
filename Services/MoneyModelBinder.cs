using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PravaMarkaz.Services;

/// <summary>
/// Summalar "1 500 000" ko'rinishida (bo'sh joy bilan) kiritiladi —
/// long qiymatlarni bo'sh joy va vergullarsiz o'qiydi.
/// </summary>
public class MoneyModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var value = ctx.ValueProvider.GetValue(ctx.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        ctx.ModelState.SetModelValue(ctx.ModelName, value);

        var raw = new string((value.FirstValue ?? "").Where(c => !char.IsWhiteSpace(c) && c != ',' && c != ' ').ToArray());
        if (raw.Length == 0)
        {
            if (Nullable.GetUnderlyingType(ctx.ModelType) != null) ctx.Result = ModelBindingResult.Success(null);
            else ctx.ModelState.TryAddModelError(ctx.ModelName, "Summani kiriting");
            return Task.CompletedTask;
        }

        if (long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
            ctx.Result = ModelBindingResult.Success(number);
        else
            ctx.ModelState.TryAddModelError(ctx.ModelName, "Summa noto'g'ri kiritilgan");
        return Task.CompletedTask;
    }
}

public class MoneyModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.ModelType == typeof(long) || context.Metadata.ModelType == typeof(long?) ? new MoneyModelBinder() : null;
}
