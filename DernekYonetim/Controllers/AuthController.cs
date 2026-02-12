using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;

namespace DernekYonetim.Controllers
{
    public class AuthController : Controller
    {
        private readonly DernekYonetimContext _context;

        public AuthController(DernekYonetimContext context)
        {
            _context = context;
        }

        // LOGIN
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string kullaniciAdi, string sifre)
        {
            var admin = _context.AdminKullanicilars
                .FirstOrDefault(x => x.KullaniciAdi == kullaniciAdi && x.AktifMi == true);

            if (admin == null || admin.SifreHash != sifre)
            {
                ViewBag.Hata = "Kullanıcı adı veya şifre hatalı";
                return View();
            }

            HttpContext.Session.SetInt32("AdminID", admin.AdminId);
            HttpContext.Session.SetString("AdminAd", admin.AdSoyad ?? "");

            return RedirectToAction("Index", "Uyeler");
        }

        // REGISTER
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Adminler = _context.AdminKullanicilars.ToList();
            return View(new AdminKullanicilar());
        }

        [HttpPost]
        public IActionResult Register(AdminKullanicilar model)
        {
            if (string.IsNullOrEmpty(model.KullaniciAdi))
            {
                ViewBag.Hata = "Kullanıcı adı boş geliyor!";
                ViewBag.Adminler = _context.AdminKullanicilars.ToList();
                return View(model);
            }

            if (_context.AdminKullanicilars.Any(x => x.KullaniciAdi == model.KullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanıcı adı zaten var";
                ViewBag.Adminler = _context.AdminKullanicilars.ToList();
                return View(model);
            }

            model.AktifMi = true;
            model.KayitTarihi = DateTime.Now;

            _context.AdminKullanicilars.Add(model);
            _context.SaveChanges();

            return RedirectToAction("Register");
        }



    }
}
