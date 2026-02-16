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
            // Sayfa ilk açıldığında tabloyu doldurmak için listeyi çekiyoruz
            ViewBag.Adminler = _context.AdminKullanicilars
                                       .OrderByDescending(x => x.AdminId) // En son eklenen en üstte
                                       .ToList();

            return View(new AdminKullanicilar());
        }

        [HttpPost]
        public IActionResult Register(AdminKullanicilar model)
        {
            // 1. Validasyon Kontrolü
            if (!ModelState.IsValid)
            {
                ViewBag.Hata = "Lütfen bilgileri eksiksiz doldurun.";
                // Hata varsa listeyi tekrar doldurup sayfayı geri döndür (Tablo kaybolmasın)
                ViewBag.Adminler = _context.AdminKullanicilars.OrderByDescending(x => x.AdminId).ToList();
                return View(model);
            }

            // 2. Kullanıcı Adı Kontrolü
            if (_context.AdminKullanicilars.Any(x => x.KullaniciAdi == model.KullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanıcı adı zaten alınmış.";
                // Hata varsa listeyi tekrar doldur
                ViewBag.Adminler = _context.AdminKullanicilars.OrderByDescending(x => x.AdminId).ToList();
                return View(model);
            }

            // 3. Kayıt İşlemi
            model.AktifMi = true;
            model.KayitTarihi = DateTime.Now;

            _context.AdminKullanicilars.Add(model);
            _context.SaveChanges();

            // 4. Yönlendirme
            // EĞER: Kayıt olduktan sonra tabloyu bu sayfada görmek istiyorsan Redirect YAPMA.
            // Şöyle yaparsan sayfada kalır ve yeni eklenen kişiyi tabloda görürsün:

            ViewBag.Adminler = _context.AdminKullanicilars.OrderByDescending(x => x.AdminId).ToList();
            ModelState.Clear(); // Form kutularını temizler
            ViewBag.Hata = null; // Varsa hatayı siler

            // Başarılı mesajı eklemek istersen View'a bir alan daha ekleyebilirsin veya ViewBag.Hata'yı success gibi kullanabilirsin.
            return View(new AdminKullanicilar());

            // EĞER: Direkt Login'e atmak istiyorsan eski kodun kalabilir:
            // return RedirectToAction("Login");
        }


        // ÇIKIŞ YAP (LOGOUT)
        public IActionResult Logout()
        {
            // Oturumdaki tüm verileri (AdminID, AdSoyad vs.) siler
            HttpContext.Session.Clear();

            // Kullanıcıyı Ana Sayfaya (veya istersen Login'e) gönderir
            return RedirectToAction("Index", "Anasayfa");
        }
    }
}