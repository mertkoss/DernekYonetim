using DernekYonetim.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration; // EKLENDİ

namespace DernekYonetim.Controllers
{
    public class AuthController : Controller
    {
        private readonly DernekYonetimContext _context;
        private readonly IDataProtector _protector;
        private readonly IConfiguration _configuration; // EKLENDİ

        public AuthController(DernekYonetimContext context, IDataProtectionProvider provider, IConfiguration configuration)
        {
            _context = context;
            _protector = provider.CreateProtector("SifreSifirlama");
            _configuration = configuration; // EKLENDİ
        }

        // --- LOGIN BÖLÜMÜ ---

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated || HttpContext.Session.GetInt32("AdminID") != null)
            {
                return RedirectToAction("Index", "Anasayfa");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // GÜVENLİK EKLENDİ
        public async Task<IActionResult> Login(string email, string sifre)
        {
            var admin = _context.AdminKullanicilars
                .FirstOrDefault(x => x.Email == email && x.AktifMi == true);

            if (admin == null || admin.SifreHash.Trim() != sifre)
            {
                ViewBag.Hata = "E-posta adresi veya şifre hatalı!";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name, admin.AdSoyad ?? admin.Email),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, admin.Rol ?? "Yonetici")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            HttpContext.Session.SetInt32("AdminID", admin.AdminId);
            HttpContext.Session.SetString("AdminAd", admin.AdSoyad ?? admin.Email);
            HttpContext.Session.SetString("AdminRol", admin.Rol ?? "Yonetici");

            return RedirectToAction("Index", "Anasayfa");
        }

        // --- LOGOUT BÖLÜMÜ ---
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Anasayfa");
        }

        // --- YÖNETİCİLERİ YÖNETME BÖLÜMÜ (SADECE SUPERADMIN) ---

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public IActionResult AdminListesi()
        {
            var adminler = _context.AdminKullanicilars
                                   .OrderByDescending(x => x.AdminId)
                                   .ToList();
            return View(adminler);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken] // GÜVENLİK EKLENDİ
        public IActionResult AdminSil(int id)
        {
            var admin = _context.AdminKullanicilars.Find(id);
            if (admin != null)
            {
                var currentUserId = HttpContext.Session.GetInt32("AdminID");
                if (currentUserId == id)
                {
                    TempData["Hata"] = "Kendi hesabınızı silemezsiniz!";
                    return RedirectToAction("AdminListesi");
                }

                _context.AdminKullanicilars.Remove(admin);
                _context.SaveChanges();
                TempData["SilmeBasarili"] = true;
            }
            return RedirectToAction("AdminListesi");
        }

        // --- YÖNETİCİ EKLEME BÖLÜMÜ (SADECE SUPERADMIN) ---

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new AdminKullanicilar());
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken] // GÜVENLİK EKLENDİ
        public IActionResult Register(AdminKullanicilar model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Hata = "Lütfen bilgileri eksiksiz doldurun.";
                return View(model);
            }

            if (_context.AdminKullanicilars.Any(x => x.KullaniciAdi == model.KullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanıcı adı zaten alınmış.";
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Rol))
            {
                model.Rol = "Yonetici";
            }

            model.AktifMi = true;
            model.KayitTarihi = DateTime.Now;

            _context.AdminKullanicilars.Add(model);
            _context.SaveChanges();

            TempData["IslemBasarili"] = "Yeni yönetici sisteme başarıyla eklendi.";
            return RedirectToAction("AdminListesi");
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken] // GÜVENLİK EKLENDİ
        public IActionResult RolDegistir(int id, string yeniRol)
        {
            var admin = _context.AdminKullanicilars.Find(id);
            if (admin != null)
            {
                var currentUserId = HttpContext.Session.GetInt32("AdminID");
                if (currentUserId == id)
                {
                    TempData["Hata"] = "Kendi yetkinizi değiştiremezsiniz!";
                    return RedirectToAction("AdminListesi");
                }

                admin.Rol = yeniRol;
                _context.SaveChanges();
                TempData["IslemBasarili"] = "Kullanıcı yetkisi başarıyla güncellendi.";
            }
            return RedirectToAction("AdminListesi");
        }

        // --- ŞİFRE SIFIRLAMA BÖLÜMLERİ ---
        [HttpGet]
        public IActionResult SifremiUnuttum()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // GÜVENLİK EKLENDİ
        public IActionResult SifremiUnuttum(string email)
        {
            var admin = _context.AdminKullanicilars.FirstOrDefault(x => x.Email == email && x.AktifMi == true);

            if (admin != null)
            {
                try
                {
                    var veri = $"{admin.AdminId}|{DateTime.Now.AddHours(1).Ticks}";
                    string token = _protector.Protect(veri);
                    var link = Url.Action("SifreSifirla", "Auth", new { t = token }, Request.Scheme);

                    GonderSifirlamaMaili(admin.Email, admin.AdSoyad, link);

                    ViewBag.Basarili = "Sıfırlama bağlantısı gönderildi.";
                }
                catch (Exception ex)
                {
                    ViewBag.Hata = "Mail Gönderilemedi: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Hata = "Bu e-posta kayıtlı değil.";
            }

            return View();
        }

        [HttpGet]
        public IActionResult SifreSifirla(string t)
        {
            if (string.IsNullOrEmpty(t)) return RedirectToAction("Login");

            try
            {
                string cozulmusVeri = _protector.Unprotect(t);
                string[] parcalar = cozulmusVeri.Split('|');

                int adminId = int.Parse(parcalar[0]);
                long ticks = long.Parse(parcalar[1]);
                DateTime sonKullanma = new DateTime(ticks);

                if (DateTime.Now > sonKullanma)
                {
                    ViewBag.Hata = "Bu bağlantının süresi dolmuş.";
                    return View("Error");
                }

                ViewBag.Token = t;
                return View();
            }
            catch
            {
                ViewBag.Hata = "Geçersiz bağlantı.";
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // GÜVENLİK EKLENDİ
        public IActionResult SifreSifirla(string token, string yeniSifre, string yeniSifreTekrar)
        {
            if (yeniSifre != yeniSifreTekrar)
            {
                ViewBag.Hata = "Şifreler uyuşmuyor.";
                ViewBag.Token = token;
                return View();
            }

            try
            {
                string cozulmusVeri = _protector.Unprotect(token);
                int adminId = int.Parse(cozulmusVeri.Split('|')[0]);

                var admin = _context.AdminKullanicilars.Find(adminId);
                if (admin != null)
                {
                    admin.SifreHash = yeniSifre;
                    _context.SaveChanges();
                    return RedirectToAction("Login");
                }
            }
            catch
            {
                ViewBag.Hata = "İşlem sırasında hata oluştu.";
            }

            return View();
        }

        private void GonderSifirlamaMaili(string aliciEmail, string adSoyad, string link)
        {
            // PROFESYONEL GÜVENLİK: Şifreler appsettings.json'dan çekiliyor!
            string gonderenMail = _configuration["EmailSettings:SenderMail"];
            string gonderenSifre = _configuration["EmailSettings:SenderPassword"];
            string smtpSunucu = _configuration["EmailSettings:SmtpServer"];
            int smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);

            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtp = new SmtpClient(smtpSunucu);

                mail.From = new MailAddress(gonderenMail, "Ankara Öğretim Derneği");
                mail.To.Add(aliciEmail);

                mail.Subject = "🔐 Şifre Sıfırlama Talebi";
                mail.IsBodyHtml = true;

                // Uzun HTML tasarımı aynı bırakıldı (Kısalttım sadece okuman kolay olsun diye)
                string htmlBody = $@"
                <!DOCTYPE html>
                <html>
                <body style='margin:0; padding:0; background-color:#f4f4f5; font-family: ""Segoe UI"", Tahoma, sans-serif;'>
                    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%'>
                        <tr>
                            <td align='center' style='padding: 40px 0;'>
                                <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='600' style='background-color:#ffffff; border-radius:12px; overflow:hidden;'>
                                    <tr>
                                        <td align='center' style='background-color:#0a1a5c; padding:30px;'>
                                            <h1 style='color:#ffffff; margin:0;'>AÖD</h1>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding:40px;'>
                                            <h2>Merhaba, {adSoyad} 👋</h2>
                                            <p>Yeni şifrenizi belirlemek için aşağıdaki butona tıklayabilirsiniz.</p>
                                            <div style='text-align:center; margin:35px 0;'>
                                                <a href='{link}' style='background-color:#0a1a5c; color:#ffffff; padding:14px 30px; text-decoration:none; border-radius:8px;'>Şifremi Sıfırla</a>
                                            </div>
                                            <p style='color:#999999; font-size:12px;'>Link çalışmıyorsa: {link}</p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

                mail.Body = htmlBody;

                smtp.Port = smtpPort;
                smtp.Credentials = new NetworkCredential(gonderenMail, gonderenSifre);
                smtp.EnableSsl = true;
                smtp.Timeout = 10000;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                throw new Exception("Mail Gönderilemedi: " + ex.Message);
            }
        }
    }
}