using DernekYonetim.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies; // 1. BU EKLENDÝ

var builder = WebApplication.CreateBuilder(args);

// 1. Session Servisini Ekle (SENÝN KODUN)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 30 dk boþta kalýrsa oturum düþer
});

// 2. Authentication (Kimlik Doðrulama) Servisini Ekle (BU EKLENDÝ)
// Giriþ yapýldý mý yapýlmadý mý takibini bu servis saðlar.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Giris/Index"; // Giriþ yapýlmamýþsa kullanýcýyý buraya yönlendir
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Oturum süresi
    });

// MVC
builder.Services.AddDataProtection();

builder.Services.AddControllersWithViews(options =>
{
    // Sistemdeki tüm Controller'lara bu log izleyicisini otomatik takýyoruz!
    options.Filters.Add<DernekYonetim.Filters.AuditLogFilter>();
});

// DbContext (DernekYonetimDB baðlantýsý)
builder.Services.AddDbContext<DernekYonetimContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DernekDB")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 2. Session Middleware'ini Aktif Et (SENÝN KODUN)
app.UseSession();

// 3. Authentication Middleware'ini Ekle (BU EKLENDÝ)
// DÝKKAT: Mutlaka UseRouting'den SONRA, UseAuthorization'dan ÖNCE olmalý.
app.UseAuthentication();

app.UseAuthorization();

// MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Anasayfa}/{action=Index}/{id?}"); // Anasayfa ayarýný korudum

app.Run();