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

        public async Task<IActionResult> Index()
        {
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