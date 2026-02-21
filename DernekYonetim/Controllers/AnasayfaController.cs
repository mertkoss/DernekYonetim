using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DernekYonetim.Controllers
{
    public class AnasayfaController : Controller
    {
        private readonly DernekYonetimContext _context;

        public AnasayfaController(DernekYonetimContext context)
        {
            _context = context;
        }

        // Üye Listesi
        // Üye Listesi (ve Ziyaretçi Sayacı)
        public IActionResult Index()
        {
            // --- 1. ZİYARETÇİ SAYACI İŞLEMLERİ ---

            // Veritabanındaki sayaç tablosunu çağırıyoruz (DbSet adınız ZiyaretciSayacis olabilir, kontrol edin)
            var sayac = _context.ZiyaretciSayacis.FirstOrDefault();

            if (sayac == null)
            {
                // Eğer tabloda hiç kayıt yoksa, ilk kaydı 1 olarak oluşturuyoruz
                sayac = new ZiyaretciSayaci { ToplamZiyaretci = 1 };
                _context.ZiyaretciSayacis.Add(sayac);
            }
            else
            {
                // Kayıt varsa sayıyı 1 artırıyoruz (Her F5 yapıldığında artar)
                sayac.ToplamZiyaretci++;
            }

            // Değişikliği veritabanına kaydediyoruz
            _context.SaveChanges();

            // Güncel sayıyı View tarafına gönderiyoruz
            ViewBag.ZiyaretciSayisi = sayac.ToplamZiyaretci;


            // --- 2. MEVCUT ÜYE LİSTESİ İŞLEMLERİ ---
            var model = new HomeViewModel
            {
                Uyeler = _context.Uyelers.ToList()
            };

            return View(model);
        }



        // Üye Detay
        public IActionResult Detay(int id)
        {
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

        // Test Admin
        public IActionResult TestAdmin()
        {
            using SqlConnection con = new SqlConnection(
                "Server=.;Database=DernekYonetimDB;Trusted_Connection=True;");

            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM AdminKullanicilar", con);
            int sayi = (int)cmd.ExecuteScalar();

            ViewBag.AdminSayisi = sayi;
            return View();
        }

        [HttpPost]
        public IActionResult SosyalMedyaKaydet(string facebook, string twitter, string instagram, string linkedin, string youtube)
        {
            // Admin değilse at
            if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

            // Formdan gelen verileri bir sözlükte (Dictionary) topla
            var data = new Dictionary<string, string>
    {
        { "facebook", facebook ?? "" },
        { "twitter", twitter ?? "" },
        { "instagram", instagram ?? "" },
        { "linkedin", linkedin ?? "" },
        { "youtube", youtube ?? "" }
    };

            // wwwroot klasörünün içine social.json adında bir dosyaya kaydet
            string jsonPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "social.json");
            string jsonString = System.Text.Json.JsonSerializer.Serialize(data);
            System.IO.File.WriteAllText(jsonPath, jsonString);

            // Kayıt bitince, kullanıcının tıklamayı yaptığı sayfaya geri dönmesini sağlar
            string referer = Request.Headers["Referer"].ToString();
            return Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
        }
    }
}
