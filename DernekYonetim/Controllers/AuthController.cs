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

namespace DernekYonetim.Controllers
{
    public class AuthController : Controller
    {
        private readonly DernekYonetimContext _context;
        private readonly IDataProtector _protector;

        public AuthController(DernekYonetimContext context, IDataProtectionProvider provider)
        {
            _context = context;
            _protector = provider.CreateProtector("SifreSifirlama");
        }

        // --- LOGIN BÖLÜMÜ ---

        [HttpGet]
        public IActionResult Login()
        {
            // Eğer kullanıcı zaten giriş yapmışsa (Cookie veya Session varsa) panele yönlendir
            if (User.Identity!.IsAuthenticated || HttpContext.Session.GetInt32("AdminID") != null)
            {
                return RedirectToAction("Index", "Uyeler");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string sifre)
        {
            var admin = _context.AdminKullanicilars
                .FirstOrDefault(x => x.Email == email && x.AktifMi == true);

            if (admin == null || admin.SifreHash.Trim() != sifre)
            {
                ViewBag.Hata = "E-posta adresi veya şifre hatalı!";
                return View();
            }

            // 1. GÜVENLİK: Çerez (Cookie) ve Claim tabanlı yetkilendirme (Authorize etiketleri için şarttır)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name, admin.AdSoyad ?? admin.Email),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, admin.Rol ?? "Yonetici") // Rolü sisteme tanıtıyoruz!
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 2. Session (Geriye dönük uyumluluk veya view'larda isim göstermek için)
            HttpContext.Session.SetInt32("AdminID", admin.AdminId);
            HttpContext.Session.SetString("AdminAd", admin.AdSoyad ?? admin.Email);
            HttpContext.Session.SetString("AdminRol", admin.Rol ?? "Yonetici");

            return RedirectToAction("Index", "Anasayfa");
        }

        // --- LOGOUT BÖLÜMÜ ---
        public async Task<IActionResult> Logout()
        {
            // Hem çerezleri hem session'ı temizle
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Anasayfa");
        }

        // --- YÖNETİCİLERİ YÖNETME BÖLÜMÜ (SADECE SUPERADMIN) ---

        [Authorize(Roles = "SuperAdmin")] // Sadece SuperAdmin girebilir!
        [HttpGet]
        public IActionResult AdminListesi()
        {
            // Tüm yöneticileri listelemek, düzenlemek ve silmek için ayrı bir sayfa.
            var adminler = _context.AdminKullanicilars
                                   .OrderByDescending(x => x.AdminId)
                                   .ToList();
            return View(adminler);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public IActionResult AdminSil(int id)
        {
            var admin = _context.AdminKullanicilars.Find(id);
            if (admin != null)
            {
                // Ekstra Güvenlik: SuperAdmin'in kendi kendini yanlışlıkla silmesini engelle
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
            // Artık manuel Session kontrolüne gerek yok, [Authorize] bunu hallediyor.
            return View(new AdminKullanicilar());
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
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

            // Eğer formdan rol seçilmemişse, güvenlik için varsayılan olarak normal "Yonetici" yap.
            if (string.IsNullOrEmpty(model.Rol))
            {
                model.Rol = "Yonetici";
            }

            model.AktifMi = true;
            model.KayitTarihi = DateTime.Now;

            _context.AdminKullanicilars.Add(model);
            _context.SaveChanges();

            TempData["KayitBasarili"] = true;

            // Kayıt başarılı olunca yöneticileri yöneteceğimiz listeye yönlendir
            return RedirectToAction("AdminListesi");
        }

        // --- ŞİFRE SIFIRLAMA BÖLÜMLERİ ---
        // (Bu kısımlarda bir değişiklik yapmadım, mevcut kodların gayet iyi çalışıyor)

        [HttpGet]
        public IActionResult SifremiUnuttum()
        {
            return View();
        }

        [HttpPost]
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
                    if (ex.InnerException != null)
                    {
                        ViewBag.Hata += " Detay: " + ex.InnerException.Message;
                    }
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
            // --- GÖNDERİCİ AYARLARI (POSTACI) ---
            string gonderenMail = "mertkosar153@gmail.com";
            string gonderenSifre = "cnvx rfyd bdnq nmqb";

            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtp = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress(gonderenMail, "Ankara Öğretim Derneği");
                mail.To.Add(aliciEmail);

                // Konu başlığına dikkat çekici bir ikon ekledim
                mail.Subject = "🔐 Şifre Sıfırlama Talebi";
                mail.IsBodyHtml = true;

                // --- PROFESYONEL HTML TASARIM ---
                string htmlBody = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                @media only screen and (max-width: 600px) {{
                    .container {{ width: 100% !important; }}
                    .content {{ padding: 20px !important; }}
                }}
            </style>
        </head>
        <body style='margin:0; padding:0; background-color:#f4f4f5; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
            
            <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%'>
                <tr>
                    <td align='center' style='padding: 40px 0;'>
                        
                        <table class='container' role='presentation' border='0' cellpadding='0' cellspacing='0' width='600' style='background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                            
                            <tr>
                                <td align='center' style='background-color:#0a1a5c; padding:30px;'>
                                    <h1 style='color:#ffffff; margin:0; font-size:24px; font-weight:700; letter-spacing: 1px;'>AÖD</h1>
                                    <p style='color:#b8860b; margin:5px 0 0 0; font-size:12px; font-weight:600; text-transform:uppercase;'>Ankara Öğretim Derneği</p>
                                </td>
                            </tr>

                            <tr>
                                <td class='content' style='padding:40px;'>
                                    <h2 style='color:#333333; font-size:20px; margin-top:0;'>Merhaba, {adSoyad} 👋</h2>
                                    
                                    <p style='color:#666666; font-size:15px; line-height:1.6; margin-bottom:25px;'>
                                        Hesabınız için bir şifre sıfırlama talebi aldık. Yeni şifrenizi belirlemek için aşağıdaki butona tıklayabilirsiniz.
                                    </p>

                                    <div style='text-align:center; margin:35px 0;'>
                                        <a href='{link}' style='background-color:#0a1a5c; color:#ffffff; padding:14px 30px; text-decoration:none; font-weight:bold; border-radius:8px; font-size:16px; display:inline-block; border-bottom: 4px solid #051030;'>
                                            Şifremi Sıfırla
                                        </a>
                                    </div>

                                    <p style='color:#666666; font-size:14px; line-height:1.6;'>
                                        Bu işlemi siz yapmadıysanız, bu e-postayı güvenle silebilirsiniz. Hesabınız güvendedir.
                                    </p>

                                    <div style='margin-top:30px; padding-top:20px; border-top:1px solid #eeeeee;'>
                                        <p style='font-size:12px; color:#999999; margin:0;'>
                                            Link çalışmıyorsa: <br>
                                            <a href='{link}' style='color:#0a1a5c; word-break:break-all;'>{link}</a>
                                        </p>
                                    </div>
                                </td>
                            </tr>

                            <tr>
                                <td style='background-color:#f8fafc; padding:20px; text-align:center; border-top:1px solid #e2e8f0;'>
                                    <p style='font-size:12px; color:#94a3b8; margin:0;'>
                                        &copy; {DateTime.Now.Year} Ankara Öğretim Derneği
                                    </p>
                                </td>
                            </tr>

                        </table>
                    </td>
                </tr>
            </table>

        </body>
        </html>";

                mail.Body = htmlBody;

                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential(gonderenMail, gonderenSifre);
                smtp.EnableSsl = true;

                // Timeout ekliyoruz: 10 saniye içinde gönderemezse dönmeyi bıraksın hata versin.
                smtp.Timeout = 10000;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                // Hata alırsak dönmeyi durdurup hatayı göstersin
                throw new Exception("Mail Gönderilemedi: " + ex.Message);
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public IActionResult RolDegistir(int id, string yeniRol)
        {
            var admin = _context.AdminKullanicilars.Find(id);
            if (admin != null)
            {
                // Güvenlik: SuperAdmin kendi rolünü yanlışlıkla düşüremesin
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
    }
}