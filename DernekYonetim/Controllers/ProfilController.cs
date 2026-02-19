using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DernekYonetim.Models; // Kendi model namespace'in

public class ProfilController : Controller
{
    private readonly DernekYonetimContext _context;

    public ProfilController(DernekYonetimContext context)
    {
        _context = context;
    }

    // SAYFAYI GÖRÜNTÜLEME (GET)
    public async Task<IActionResult> Index()
    {
        // 1. Session'dan giriş yapan kişinin ID'sini al
        var adminId = HttpContext.Session.GetInt32("AdminID");
        if (adminId == null) return RedirectToAction("Login", "Auth");

        // 2. Veritabanından o anki kullanıcının güncel verilerini çek
        var aktifKullanici = await _context.AdminKullanicilars.FindAsync(adminId);

        if (aktifKullanici == null)
        {
            // Kullanıcı silinmişse veya bulunamadıysa sistemden at
            return RedirectToAction("Logout", "Auth");
        }

        return View(aktifKullanici);
    }

    // BİLGİLERİ GÜNCELLEME (POST)
    [HttpPost]
    public async Task<IActionResult> Index(AdminKullanicilar guncelVeri)
    {
        var adminId = HttpContext.Session.GetInt32("AdminID");
        if (adminId == null) return RedirectToAction("Login", "Auth");

        var mevcutKullanici = await _context.AdminKullanicilars.FindAsync(adminId);
        if (mevcutKullanici != null)
        {
            // Alanları ayrı ayrı güncelliyoruz
            mevcutKullanici.AdSoyad = guncelVeri.AdSoyad;
            mevcutKullanici.KullaniciAdi = guncelVeri.KullaniciAdi;
            mevcutKullanici.Email = guncelVeri.Email;

            _context.AdminKullanicilars.Update(mevcutKullanici);
            await _context.SaveChangesAsync();

            // Navbar'da ismin güncellenmesi için Session'a AdSoyad'ı atıyoruz
            HttpContext.Session.SetString("AdminAd", mevcutKullanici.AdSoyad);

            TempData["ProfilGuncellendi"] = "Profil bilgileriniz başarıyla güncellendi.";
        }

        return RedirectToAction("Index");
    }
}