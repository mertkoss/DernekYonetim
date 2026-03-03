using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DernekYonetim.Models;

public class ProfilController : Controller
{
    private readonly DernekYonetimContext _context;

    public ProfilController(DernekYonetimContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var adminId = HttpContext.Session.GetInt32("AdminID");
        if (adminId == null) return RedirectToAction("Login", "Auth");

        var aktifKullanici = await _context.AdminKullanicilars.FindAsync(adminId);

        if (aktifKullanici == null)
        {
            return RedirectToAction("Logout", "Auth");
        }

        return View(aktifKullanici);
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // GÜVENLİK EKLENDİ
    public async Task<IActionResult> Index(AdminKullanicilar guncelVeri)
    {
        var adminId = HttpContext.Session.GetInt32("AdminID");
        if (adminId == null) return RedirectToAction("Login", "Auth");

        var mevcutKullanici = await _context.AdminKullanicilars.FindAsync(adminId);
        if (mevcutKullanici != null)
        {
            mevcutKullanici.AdSoyad = guncelVeri.AdSoyad;
            mevcutKullanici.KullaniciAdi = guncelVeri.KullaniciAdi;
            mevcutKullanici.Email = guncelVeri.Email;

            _context.AdminKullanicilars.Update(mevcutKullanici);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("AdminAd", mevcutKullanici.AdSoyad);

            // TÜM PROJEYLE STANDART OLMASI İÇİN "Basari" OLARAK DEĞİŞTİRİLDİ
            TempData["Basari"] = "Profil bilgileriniz başarıyla güncellendi.";
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult SifreDegistir()
    {
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // GÜVENLİK EKLENDİ
    public async Task<IActionResult> SifreDegistir(string eskiSifre, string yeniSifre, string yeniSifreTekrar)
    {
        var adminId = HttpContext.Session.GetInt32("AdminID");
        if (adminId == null) return RedirectToAction("Login", "Auth");

        var mevcutKullanici = await _context.AdminKullanicilars.FindAsync(adminId);
        if (mevcutKullanici == null) return RedirectToAction("Logout", "Auth");

        // PRG PATTERN EKLENDİ (ViewBag yerine TempData ve RedirectToAction)
        if (mevcutKullanici.SifreHash.Trim() != eskiSifre)
        {
            TempData["Hata"] = "Mevcut şifrenizi yanlış girdiniz.";
            return RedirectToAction("SifreDegistir");
        }

        if (yeniSifre != yeniSifreTekrar)
        {
            TempData["Hata"] = "Yeni girdiğiniz şifreler birbiriyle uyuşmuyor.";
            return RedirectToAction("SifreDegistir");
        }

        if (yeniSifre.Length < 6)
        {
            TempData["Hata"] = "Yeni şifreniz en az 6 karakter uzunluğunda olmalıdır.";
            return RedirectToAction("SifreDegistir");
        }

        mevcutKullanici.SifreHash = yeniSifre;
        _context.AdminKullanicilars.Update(mevcutKullanici);
        await _context.SaveChangesAsync();

        TempData["Basari"] = "Şifreniz güvenlik standartlarına uygun olarak başarıyla güncellendi.";
        return RedirectToAction("SifreDegistir");
    }
}