using DernekYonetim.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Session Servisini Ekle (BURASI EKLENDÝ)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 30 dk boþta kalýrsa oturum düþer
});

// MVC
builder.Services.AddControllersWithViews();

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

// 2. Session Middleware'ini Aktif Et (BURASI EKLENDÝ)
// ÖNEMLÝ: UseRouting'den sonra, UseAuthorization'dan önce olmalý.
app.UseSession();

app.UseAuthorization();

// MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Anasayfa}/{action=Index}/{id?}"); // Anasayfa ayarýný korudum

app.Run();