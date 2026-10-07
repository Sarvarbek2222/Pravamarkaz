using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using PravaMarkaz.Data;
using PravaMarkaz.Models;
using PravaMarkaz.Services;

var builder = WebApplication.CreateBuilder(args);

// Sanalar va sonlar server tilidan qat'i nazar bir xil ishlashi uchun
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<Audit>();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("appsettings.json da ConnectionStrings:Default ko'rsatilmagan");
var serverVersion = ServerVersion.Parse(builder.Configuration["Database:ServerVersion"] ?? "8.0.36-mysql");
builder.Services.AddDbContext<AppDbContext>(o => o.UseMySql(connectionString, serverVersion));

// Yuklangan rasmlar va cookie kalitlari uchun papka
var dataDir = builder.Configuration["Storage:DataPath"];
if (string.IsNullOrWhiteSpace(dataDir))
    dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
builder.Services.AddSingleton(new PhotoStorage(Path.Combine(dataDir, "uploads")));
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")))
    .SetApplicationName("PravaMarkaz");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/Denied";
        o.Cookie.Name = "PravaMarkaz.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;

        // Foydalanuvchi bloklansa yoki paroli/roli o'zgarsa — eski sessiya darhol bekor bo'ladi
        o.Events.OnValidatePrincipal = async ctx =>
        {
            var idText = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var stamp = ctx.Principal?.FindFirstValue(CurrentUser.StampClaim);
            using var scope = ctx.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ok = int.TryParse(idText, out var id) &&
                     await db.Users.AnyAsync(u => u.Id == id && u.IsActive && u.SecurityStamp == stamp);
            if (!ok)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Owner", p => p.RequireRole(nameof(UserRole.Owner)));
});

builder.Services.AddControllersWithViews(o =>
{
    // Hamma sahifalar faqat tizimga kirganlarga ochiq
    o.Filters.Add(new AuthorizeFilter());
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    o.ModelBinderProviders.Insert(0, new MoneyModelBinderProvider());
});

// Nginx / reverse proxy ortida ishlash uchun
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Bazani yaratish / yangilash va birinchi foydalanuvchini (egasi) qo'shish
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        var user = new AppUser
        {
            Username = app.Configuration["Admin:Username"] ?? "admin",
            FullName = app.Configuration["Admin:FullName"] ?? "Administrator",
            Role = UserRole.Owner
        };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, app.Configuration["Admin:Password"] ?? "admin123");
        db.Users.Add(user);
        db.SaveChanges();
    }

    // Namunaviy ma'lumotlar: dotnet run -- --seed-demo
    if (args.Contains("--seed-demo"))
    {
        DemoSeeder.Seed(db);
        return;
    }
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

if (app.Configuration.GetValue<bool>("UseHttpsRedirection"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
