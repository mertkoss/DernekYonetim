using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // IConfiguration için eklendi
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DernekYonetim.Controllers
{
    public class AnasayfaController : Controller
    {
        private readonly DernekYonetimContext _context;
        private readonly IConfiguration _configuration; // JSON okumak için tanımladık

        // IConfiguration'ı Constructor'a ekledik
        public AnasayfaController(DernekYonetimContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            // ---- ZİYARETÇİ SAYACI LOGIC ----
            if (HttpContext.Session.GetString("ZiyaretEdildi") == null)
            {
                var sayac = await _context.ZiyaretciSayacis.FirstOrDefaultAsync();
                if (sayac == null)
                {
                    sayac = new ZiyaretciSayaci { ToplamZiyaretci = 1 };
                    _context.ZiyaretciSayacis.Add(sayac);
                }
                else
                {
                    sayac.ToplamZiyaretci++;
                }
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("ZiyaretEdildi", "evet");
            }

            var guncelSayac = await _context.ZiyaretciSayacis.FirstOrDefaultAsync();
            ViewBag.ZiyaretciSayisi = guncelSayac?.ToplamZiyaretci ?? 0;


            // ---- YENİ EKLENEN İSTATİSTİKLER (appsettings.json'dan çekiliyor) ----
            var istatistik = _configuration.GetSection("DernekIstatistik");

            // Eğer JSON'da değer bulamazsa 0 veya varsayılan değer atasın diye kontrol yapıyoruz
            ViewBag.KurulusYili = istatistik["KurulusYili"] ?? "1965";
            ViewBag.AnkaraMezunSayisi = istatistik["AnkaraMezunSayisi"] ?? "0";
            ViewBag.IzmirMezunSayisi = istatistik["IzmirMezunSayisi"] ?? "0";


            // ---- VİEW MODEL DOLDURMA ----
            var model = new HomeViewModel
            {
                Uyeler = await _context.Uyelers.ToListAsync(),
                About = await _context.DernekHakkindaBolumleris.FirstOrDefaultAsync(),

                // Son 4 Haber
                SonHaberler = await _context.Haberlers
                    .Include(h => h.Kategori)
                    .OrderByDescending(h => h.YayimTarihi)
                    .Take(4)
                    .ToListAsync(),

                SliderHaberler = await _context.Haberlers
                    .Where(h => h.SlayttaGoster)
                    .OrderByDescending(h => h.YayimTarihi)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        // Üye Detay
        public IActionResult Detay(int id)
        {
            if (HttpContext.Session.GetInt32("AdminID") == null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index");

            var uye = _context.Uyelers
                .Include(x => x.Aidatlars)
                .Include(x => x.DerbisKaydis)
                .FirstOrDefault(x => x.UyeId == id);

            if (uye == null)
                return NotFound();

            var model = new HomeViewModel
            {
                UyeDetay = uye
            };

            return View(model);
        }

        // PROFESYONEL VE GÜVENLİ TEST ADMIN METODU
        public async Task<IActionResult> TestAdmin()
        {
            // Test bile olsa dışarıdan erişimi kapattık
            if (HttpContext.Session.GetInt32("AdminID") == null)
                return RedirectToAction("Login", "Auth");

            // İlkel SqlConnection yerine Entity Framework kullanarak sorgu attık
            int sayi = await _context.AdminKullanicilars.CountAsync();

            ViewBag.AdminSayisi = sayi;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // GÜVENLİK EKLENDİ
        public IActionResult SosyalMedyaKaydet(string facebook, string twitter, string instagram, string linkedin, string youtube)
        {
            if (HttpContext.Session.GetInt32("AdminID") == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                var data = new Dictionary<string, string>
                {
                    { "facebook", facebook ?? "" },
                    { "twitter", twitter ?? "" },
                    { "instagram", instagram ?? "" },
                    { "linkedin", linkedin ?? "" },
                    { "youtube", youtube ?? "" }
                };

                string jsonPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "social.json");
                string jsonString = System.Text.Json.JsonSerializer.Serialize(data);
                System.IO.File.WriteAllText(jsonPath, jsonString);

                // UX GÜNCELLEMESİ: İşlem başarılı mesajı
                TempData["Basari"] = "Sosyal medya bağlantıları başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Sosyal medya kaydedilirken bir hata oluştu: " + ex.Message;
            }

            string referer = Request.Headers["Referer"].ToString();
            return Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
        }
    }
}