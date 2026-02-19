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

    // SAYFAYI GÖRÜNTÜLEME (GET) - Şifre Değiştir
    [HttpGet]
    public IActionResult SifreDegistir()
    {
        // 1. Yetki Kontrolü
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        return View();
    }

    // ŞİFREYİ GÜNCELLEME İŞLEMİ (POST)
    [HttpPost]
    public async Task<IActionResult> SifreDegistir(string eskiSifre, string yeniSifre, string yeniSifreTekrar)
    {
        var adminId = HttpContext.Session.GetInt32("AdminID");
        if (adminId == null) return RedirectToAction("Login", "Auth");

        var mevcutKullanici = await _context.AdminKullanicilars.FindAsync(adminId);
        if (mevcutKullanici == null) return RedirectToAction("Logout", "Auth");

        // 1. ESKİ ŞİFRE DOĞRULAMASI (Kritik Güvenlik Adımı)
        // Trim() kullanıyoruz çünkü AuthController'daki Login'de de kullanmışsın
        if (mevcutKullanici.SifreHash.Trim() != eskiSifre)
        {
            ViewBag.Hata = "Mevcut şifrenizi yanlış girdiniz.";
            return View();
        }

        // 2. YENİ ŞİFRELERİN UYUŞMA KONTROLÜ
        if (yeniSifre != yeniSifreTekrar)
        {
            ViewBag.Hata = "Yeni girdiğiniz şifreler birbiriyle uyuşmuyor.";
            return View();
        }

        // 3. ŞİFRE GÜVENLİK KONTROLÜ (Opsiyonel ama profesyonel)
        if (yeniSifre.Length < 6)
        {
            ViewBag.Hata = "Yeni şifreniz en az 6 karakter uzunluğunda olmalıdır.";
            return View();
        }

        // 4. ŞİFREYİ GÜNCELLE VE KAYDET
        mevcutKullanici.SifreHash = yeniSifre;
        _context.AdminKullanicilars.Update(mevcutKullanici);
        await _context.SaveChangesAsync();

        // 5. İŞLEM BAŞARILI MESAJI
        ViewBag.Basarili = "Şifreniz güvenlik standartlarına uygun olarak başarıyla güncellendi.";

        // Yönlendirme yapmıyoruz, aynı sayfada kalıp yeşil başarı kutusunu gösteriyoruz
        return View();
    }
}