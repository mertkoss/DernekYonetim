using DernekYonetim.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http; // Session için gerekli
using Microsoft.AspNetCore.Mvc;
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
            // 1. GÜVENLİK KİLİDİ: Giriş yapmayan kişi sayfayı (URL'den yazsa bile) açamaz
            if (HttpContext.Session.GetInt32("AdminID") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Sayfa ilk açıldığında tabloyu doldurmak için listeyi çekiyoruz
            ViewBag.Adminler = _context.AdminKullanicilars
                                       .OrderByDescending(x => x.AdminId) // En son eklenen en üstte
                                       .ToList();

            return View(new AdminKullanicilar());
        }

        [HttpPost]
        public IActionResult Register(AdminKullanicilar model)
        {
            // 2. GÜVENLİK KİLİDİ: Giriş yapmayan kişi post isteği (kayıt) gönderemez
            if (HttpContext.Session.GetInt32("AdminID") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 1. Validasyon Kontrolü
            if (!ModelState.IsValid)
            {
                ViewBag.Hata = "Lütfen bilgileri eksiksiz doldurun.";
                ViewBag.Adminler = _context.AdminKullanicilars.OrderByDescending(x => x.AdminId).ToList();
                return View(model);
            }

            // 2. Kullanıcı Adı Kontrolü
            if (_context.AdminKullanicilars.Any(x => x.KullaniciAdi == model.KullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanıcı adı zaten alınmış.";
                ViewBag.Adminler = _context.AdminKullanicilars.OrderByDescending(x => x.AdminId).ToList();
                return View(model);
            }

            // 3. Kayıt İşlemi
            model.AktifMi = true;
            model.KayitTarihi = DateTime.Now;

            _context.AdminKullanicilars.Add(model);
            _context.SaveChanges();

            // --- PROFESYONEL YÖNLENDİRME ---
            TempData["KayitBasarili"] = true;

            // BURASI ÖNEMLİ: Artık Login'e gitmiyoruz. 
            // Mevcut admin başka bir admin eklediği için aynı sayfayı yeniliyoruz.
            return RedirectToAction("Register");
        }


        // ÇIKIŞ YAP (LOGOUT)
        public IActionResult Logout()
        {
            // Oturumdaki tüm verileri (AdminID, AdSoyad vs.) siler
            HttpContext.Session.Clear();

            // Kullanıcıyı Ana Sayfaya (veya istersen Login'e) gönderir
            return RedirectToAction("Index", "Anasayfa");
        }

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
                    // Token oluşturma işlemleri...
                    var veri = $"{admin.AdminId}|{DateTime.Now.AddHours(1).Ticks}";
                    string token = _protector.Protect(veri);
                    var link = Url.Action("SifreSifirla", "Auth", new { t = token }, Request.Scheme);

                    // Mail göndermeyi deniyoruz
                    GonderSifirlamaMaili(admin.Email, admin.AdSoyad, link);

                    // Hata çıkmazsa burası çalışır
                    ViewBag.Basarili = "Sıfırlama bağlantısı gönderildi.";
                }
                catch (Exception ex)
                {
                    // HATA VARSA ARTIK GÖRECEĞİZ
                    // ex.Message size hatanın sebebini söyleyecek
                    ViewBag.Hata = "Mail Gönderilemedi: " + ex.Message;

                    // Eğer InnerException varsa onu da görelim (daha detaylı bilgi için)
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
                // 1. Token'ı çözmeye çalış
                string cozulmusVeri = _protector.Unprotect(t);
                string[] parcalar = cozulmusVeri.Split('|');

                int adminId = int.Parse(parcalar[0]);
                long ticks = long.Parse(parcalar[1]);
                DateTime sonKullanma = new DateTime(ticks);

                // 2. Süre kontrolü
                if (DateTime.Now > sonKullanma)
                {
                    ViewBag.Hata = "Bu bağlantının süresi dolmuş.";
                    return View("Error");
                }

                // 3. Kullanıcıyı bul ve View'a ID ile Token'ı gönder
                // ID'yi hidden input'a koymak yerine TempData veya Model ile taşıyabiliriz.
                // Güvenlik için token'ı tekrar taşıyoruz.
                ViewBag.Token = t;
                return View();
            }
            catch
            {
                // Token üzerinde oynama yapılmışsa Unprotect hata fırlatır.
                ViewBag.Hata = "Geçersiz bağlantı.";
                return View("Error"); // Veya Login'e atın
            }
        }

        [HttpPost]
        public IActionResult SifreSifirla(string token, string yeniSifre, string yeniSifreTekrar)
        {
            if (yeniSifre != yeniSifreTekrar)
            {
                ViewBag.Hata = "Şifreler uyuşmuyor.";
                ViewBag.Token = token; // Hata olunca token kaybolmasın
                return View();
            }

            try
            {
                // Token'ı tekrar çözüp ID'yi alıyoruz (Hidden inputtan ID almak güvensizdir)
                string cozulmusVeri = _protector.Unprotect(token);
                int adminId = int.Parse(cozulmusVeri.Split('|')[0]);
                // Süreye tekrar bakmaya gerek yok ama isterseniz bakabilirsiniz.

                var admin = _context.AdminKullanicilars.Find(adminId);
                if (admin != null)
                {
                    admin.SifreHash = yeniSifre; // Hashleme yapıyorsanız burada yapın
                    _context.SaveChanges();

                    // Başarılı, Login'e yönlendir
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
    }
}