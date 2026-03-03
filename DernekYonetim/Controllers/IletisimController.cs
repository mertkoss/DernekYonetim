using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class IletisimController : Controller
{
    private readonly DernekYonetimContext _context;
    private readonly IConfiguration _configuration; 

    public IletisimController(DernekYonetimContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        var iletisim = await _context.Iletisims.FirstOrDefaultAsync();
        return View(iletisim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // GÜVENLİK
    public async Task<IActionResult> Guncelle(Iletisim model)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }
        var mevcut = await _context.Iletisims.FirstOrDefaultAsync();
        if (mevcut != null)
        {
            mevcut.Adres = model.Adres;
            mevcut.Telefon = model.Telefon;
            mevcut.Eposta = model.Eposta;
            _context.Iletisims.Update(mevcut);
            await _context.SaveChangesAsync();

            TempData["Basari"] = "İletişim bilgileri başarıyla güncellendi.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MesajGonder(string AdSoyad, string Eposta, string Konu, string Mesaj)
    {
        try
        {
            string guvenliAdSoyad = System.Net.WebUtility.HtmlEncode(AdSoyad);
            string guvenliEposta = System.Net.WebUtility.HtmlEncode(Eposta);
            string guvenliKonu = System.Net.WebUtility.HtmlEncode(Konu);
            string guvenliMesaj = System.Net.WebUtility.HtmlEncode(Mesaj);

            // GÜVENLİK: Ayarları appsettings.json dosyasından okuyoruz
            string gonderenMail = _configuration["EmailSettings:SenderMail"];
            string gonderenSifre = _configuration["EmailSettings:SenderPassword"];
            string aliciMail = _configuration["EmailSettings:ReceiverMail"];
            string smtpSunucu = _configuration["EmailSettings:SmtpServer"];
            int smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);

            MailMessage mail = new MailMessage();
            SmtpClient smtp = new SmtpClient(smtpSunucu); // Smtp adresini ayarlardan aldık

            mail.From = new MailAddress(gonderenMail, "AÖD İletişim Formu");
            mail.To.Add(aliciMail);
            mail.Subject = $"📬 Yeni İletişim Formu Mesajı: {guvenliKonu}";
            mail.IsBodyHtml = true;

            mail.ReplyToList.Add(new MailAddress(guvenliEposta, guvenliAdSoyad));

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
                            <table class='container' role='presentation' border='0' cellpadding='0' cellspacing='0' width='600' style='background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                                <tr>
                                    <td align='center' style='background-color:#0a1a5c; padding:30px;'>
                                        <h1 style='color:#ffffff; margin:0; font-size:24px; font-weight:700; letter-spacing: 1px;'>AÖD</h1>
                                        <p style='color:#b8860b; margin:5px 0 0 0; font-size:12px; font-weight:600; text-transform:uppercase;'>İLETİŞİM FORMU BİLDİRİMİ</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td class='content' style='padding:40px;'>
                                        <h2 style='color:#333333; font-size:20px; margin-top:0; border-bottom: 2px solid #f8fafc; padding-bottom: 15px;'>
                                            Siteden Yeni Bir Mesajınız Var 💬
                                        </h2>
                                        <p style='color:#64748b; font-size:15px; line-height:1.6; margin-bottom:30px;'>
                                            Web sitenizin iletişim formu üzerinden yeni bir talep gönderildi. İletişim detayları aşağıdadır:
                                        </p>
                                        <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' style='margin-bottom: 30px; background-color: #f8fafc; border-radius: 8px; padding: 20px;'>
                                            <tr>
                                                <td style='padding-bottom: 15px;'>
                                                    <strong style='color:#0a1a5c; font-size:13px; text-transform:uppercase;'>Gönderen</strong><br>
                                                    <span style='color:#334155; font-size:16px; font-weight:600;'>{guvenliAdSoyad}</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style='padding-bottom: 15px;'>
                                                    <strong style='color:#0a1a5c; font-size:13px; text-transform:uppercase;'>E-Posta Adresi</strong><br>
                                                    <a href='mailto:{guvenliEposta}' style='color:#b8860b; font-size:15px; text-decoration:none; font-weight:600;'>{guvenliEposta}</a>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <strong style='color:#0a1a5c; font-size:13px; text-transform:uppercase;'>Konu</strong><br>
                                                    <span style='color:#334155; font-size:15px;'>{guvenliKonu}</span>
                                                </td>
                                            </tr>
                                        </table>
                                        <strong style='color:#0a1a5c; font-size:13px; text-transform:uppercase; display:block; margin-bottom:10px;'>Mesaj İçeriği</strong>
                                        <div style='background-color:#ffffff; padding:20px; border: 1px solid #e2e8f0; border-left: 4px solid #b8860b; border-radius:4px; color:#475569; font-size:15px; line-height:1.7;'>
                                            {guvenliMesaj.Replace("\n", "<br>")}
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='background-color:#f8fafc; padding:20px; text-align:center; border-top:1px solid #e2e8f0;'>
                                        <p style='font-size:12px; color:#94a3b8; margin:0;'>
                                            Bu e-posta Ankara Öğretim Derneği web sitesi tarafından otomatik olarak oluşturulmuştur.<br>
                                            Mesajı gönderen kişiye yanıt vermek için mail uygulamanızdan <strong>""Yanıtla""</strong> butonuna basabilirsiniz.
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

            smtp.Port = smtpPort; 
            smtp.Credentials = new NetworkCredential(gonderenMail, gonderenSifre);
            smtp.EnableSsl = true;
            smtp.Timeout = 10000;

            await smtp.SendMailAsync(mail);

            TempData["Basari"] = "Mesajınız başarıyla iletildi. En kısa sürede sizinle iletişime geçeceğiz.";
        }
        catch (Exception ex)
        {
            string hataDetayi = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            TempData["Hata"] = "Mesaj gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin. (" + hataDetayi + ")";
        }

        return RedirectToAction("Index");
    }
}