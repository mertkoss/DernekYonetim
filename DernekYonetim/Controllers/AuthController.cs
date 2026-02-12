using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Session için gerekli

namespace DernekYonetim.Controllers
{
    public class AuthController : Controller
    {
        private readonly DernekYonetimContext _context;

        public AuthController(DernekYonetimContext context)
        {
            _context = context;
        }

        // --- LOGIN BÖLÜMÜ ---

        [HttpGet]
        public IActionResult Login()
        {
            // Eğer kullanıcı zaten giriş yapmışsa direkt panele yönlendir
            if (HttpContext.Session.GetInt32("AdminID") != null)
            {
                return RedirectToAction("Index", "Uyeler");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string sifre)
        {
            // 1. E-posta adresine göre kullanıcıyı getir
            var admin = _context.AdminKullanicilars
                .FirstOrDefault(x => x.Email == email && x.AktifMi == true);

            // 2. Şifre kontrolü (Trim() metodunu koruduk, boşluk hatası olmasın diye)
            if (admin == null || admin.SifreHash.Trim() != sifre)
            {
                ViewBag.Hata = "E-posta adresi veya şifre hatalı!";
                return View();
            }

            // 3. Giriş başarılı: Session'ı doldur
            HttpContext.Session.SetInt32("AdminID", admin.AdminId);

            // Ekranda isim yoksa e-posta görünsün diye güncelledik
            HttpContext.Session.SetString("AdminAd", admin.AdSoyad ?? admin.Email);

            // 4. Anasayfaya yönlendir (BURASI DEĞİŞTİ)
            return RedirectToAction("Index", "Anasayfa");
        }

        // --- REGISTER BÖLÜMÜ ---

        [HttpGet]
        public IActionResult Register()
        {
            return View(new AdminKullanicilar());
        }

        [HttpPost]
        public IActionResult Register(AdminKullanicilar model)
        {
            if (string.IsNullOrEmpty(model.KullaniciAdi) || string.IsNullOrEmpty(model.SifreHash))
            {
                ViewBag.Hata = "Lütfen tüm alanları doldurun!";
                return View(model);
            }

            if (_context.AdminKullanicilars.Any(x => x.KullaniciAdi == model.KullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanıcı adı zaten alınmış.";
                return View(model);
            }

            model.AktifMi = true;
            model.KayitTarihi = DateTime.Now;

            _context.AdminKullanicilars.Add(model);
            _context.SaveChanges();

            // Kayıt sonrası login sayfasına yönlendirmek daha kullanıcı dostudur
            return RedirectToAction("Login");
        }

        // --- LOGOUT BÖLÜMÜ ---

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Tüm session'ı temizler
            return RedirectToAction("Login");
        }
    }
}