using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DernekYonetim.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DernekYonetim.Controllers
{
    public class HaberlerController : Controller
    {
        private readonly DernekYonetimContext _context;
        private readonly IWebHostEnvironment _env;

        public HaberlerController(DernekYonetimContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [Route("Haberler")]
        [Route("Haberler/Kategori/{kategoriId:int}")]
        public async Task<IActionResult> Index(int? kategoriId, string arama, int sayfa = 1)
        {
            int pageSize = 6;
            var sorgu = _context.Haberlers.Include(h => h.Kategori).AsQueryable();

            // 1. Arama Durumu (SADECE BAŞLIKTA ARA)
            if (!string.IsNullOrEmpty(arama))
            {
                sorgu = sorgu.Where(h => h.Baslik.Contains(arama));
                ViewBag.AramaKelimesi = arama;
            }

            // 2. Kategori Durumu ve Sayfa Başlığı
            string sayfaBasligi = "Tüm Haberler ve Duyurular";
            if (kategoriId.HasValue)
            {
                sorgu = sorgu.Where(h => h.KategoriId == kategoriId.Value);
                var seciliKategori = await _context.HaberKategorileris.FindAsync(kategoriId.Value);
                if (seciliKategori != null)
                {
                    sayfaBasligi = seciliKategori.KategoriAdi;
                }
            }

            ViewBag.SayfaBasligi = sayfaBasligi;
            ViewBag.SeciliKategori = kategoriId;

            // --- 3. SLIDER İÇERİĞİ ---
            var sliderSorgusu = _context.Haberlers.Include(h => h.Kategori).Where(h => h.SlayttaGoster == true);
            if (kategoriId.HasValue)
            {
                sliderSorgusu = sliderSorgusu.Where(h => h.KategoriId == kategoriId.Value);
            }

            ViewBag.SliderHaberler = await sliderSorgusu
                .OrderByDescending(h => h.YayimTarihi)
                .Take(3)
                .ToListAsync();

            // --- 4. LİSTE İÇERİĞİ (Sayfalama) ---
            int toplamKayit = await sorgu.CountAsync();
            var haberListesi = await sorgu
                .OrderByDescending(h => h.YayimTarihi)
                .Skip((sayfa - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.ToplamKayit = toplamKayit;
            ViewBag.MevcutSayfa = sayfa;
            ViewBag.ToplamSayfa = (int)Math.Ceiling((double)toplamKayit / pageSize);

            var kategoriler = await _context.HaberKategorileris.ToListAsync();
            ViewBag.KategoriListesi = new SelectList(kategoriler, "Id", "KategoriAdi");

            // AJAX isteği gelirse sayfanın tamamını değil, sadece partial view'i döndür
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_HaberListesiPartial", haberListesi);
            }

            return View(haberListesi);
        }

        [Route("Haberler/Detay/{id}")]
        public async Task<IActionResult> Detay(int id)
        {
            var haber = await _context.Haberlers
                .Include(h => h.Kategori)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (haber == null) return NotFound();

            var kategoriler = await _context.HaberKategorileris.ToListAsync();
            ViewBag.KategoriListesi = new SelectList(kategoriler, "Id", "KategoriAdi", haber.KategoriId);

            return View(haber);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // EKLENEN: IFormFile PanelistFotograf parametresi
        public async Task<IActionResult> HaberKaydet(Haberler model, IFormFile Fotograf, IFormFile CvDosyasi, IFormFile PanelistFotograf)
        {
            if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Index");

            try
            {
                if (Fotograf != null && Fotograf.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Fotograf.FileName);
                    string path = Path.Combine(_env.WebRootPath, "uploads/haberler", fileName);
                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "uploads/haberler"));
                    using (var stream = new FileStream(path, FileMode.Create)) { await Fotograf.CopyToAsync(stream); }
                    model.FotografYolu = "/uploads/haberler/" + fileName;
                }

                if (CvDosyasi != null && CvDosyasi.Length > 0)
                {
                    string cvFileName = Guid.NewGuid().ToString() + Path.GetExtension(CvDosyasi.FileName);
                    string cvPath = Path.Combine(_env.WebRootPath, "uploads/cv", cvFileName);
                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "uploads/cv"));
                    using (var stream = new FileStream(cvPath, FileMode.Create)) { await CvDosyasi.CopyToAsync(stream); }
                    model.CvDosyaYolu = "/uploads/cv/" + cvFileName;
                }

                // YENİ EKLENEN: Panelist Fotoğrafı Yükleme İşlemi
                if (PanelistFotograf != null && PanelistFotograf.Length > 0)
                {
                    string pFileName = Guid.NewGuid().ToString() + Path.GetExtension(PanelistFotograf.FileName);
                    string pPath = Path.Combine(_env.WebRootPath, "uploads/panelist", pFileName);
                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "uploads/panelist"));
                    using (var stream = new FileStream(pPath, FileMode.Create)) { await PanelistFotograf.CopyToAsync(stream); }
                    model.PanelistFotografYolu = "/uploads/panelist/" + pFileName;
                }

                model.YayimTarihi = DateTime.Now;
                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["Basari"] = "Haber başarıyla eklendi.";
            }
            catch (Exception) { TempData["Hata"] = "Haber eklenirken bir sorun oluştu."; }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // EKLENEN: IFormFile PanelistFotograf parametresi
        public async Task<IActionResult> HaberGuncelle(Haberler model, IFormFile Fotograf, IFormFile CvDosyasi, IFormFile PanelistFotograf)
        {
            if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Index");

            var mevcutHaber = await _context.Haberlers.FindAsync(model.Id);
            if (mevcutHaber == null) return NotFound();

            try
            {
                mevcutHaber.Baslik = model.Baslik;
                mevcutHaber.KategoriId = model.KategoriId;
                mevcutHaber.Ozet = model.Ozet;
                mevcutHaber.Icerik = model.Icerik;
                mevcutHaber.SlayttaGoster = model.SlayttaGoster;
                mevcutHaber.PanelistOzgecmis = model.PanelistOzgecmis;

                if (Fotograf != null && Fotograf.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Fotograf.FileName);
                    string path = Path.Combine(_env.WebRootPath, "uploads/haberler", fileName);
                    using (var stream = new FileStream(path, FileMode.Create)) { await Fotograf.CopyToAsync(stream); }
                    mevcutHaber.FotografYolu = "/uploads/haberler/" + fileName;
                }

                if (CvDosyasi != null && CvDosyasi.Length > 0)
                {
                    string cvFileName = Guid.NewGuid().ToString() + Path.GetExtension(CvDosyasi.FileName);
                    string cvPath = Path.Combine(_env.WebRootPath, "uploads/cv", cvFileName);
                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "uploads/cv"));
                    using (var stream = new FileStream(cvPath, FileMode.Create)) { await CvDosyasi.CopyToAsync(stream); }
                    mevcutHaber.CvDosyaYolu = "/uploads/cv/" + cvFileName;
                }

                if (PanelistFotograf != null && PanelistFotograf.Length > 0)
                {
                    string pFileName = Guid.NewGuid().ToString() + Path.GetExtension(PanelistFotograf.FileName);
                    string pPath = Path.Combine(_env.WebRootPath, "uploads/panelist", pFileName);
                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "uploads/panelist"));
                    using (var stream = new FileStream(pPath, FileMode.Create)) { await PanelistFotograf.CopyToAsync(stream); }
                    mevcutHaber.PanelistFotografYolu = "/uploads/panelist/" + pFileName;
                }

                _context.Update(mevcutHaber);
                await _context.SaveChangesAsync();
                TempData["Basari"] = "Haber başarıyla güncellendi.";
            }
            catch (Exception) { TempData["Hata"] = "Güncelleme sırasında bir hata oluştu."; }

            return RedirectToAction("Detay", new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Index");

            var haber = await _context.Haberlers.FindAsync(id);
            if (haber != null)
            {
                _context.Haberlers.Remove(haber);
                await _context.SaveChangesAsync();
                TempData["Basari"] = "Haber başarıyla silindi.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}