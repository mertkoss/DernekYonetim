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
    }
}
