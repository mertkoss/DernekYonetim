using DernekYonetim.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
app.UseAuthorization();

// MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Uyeler}/{action=Index}/{id?}");

app.Run();

