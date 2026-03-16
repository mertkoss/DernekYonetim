using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DernekYonetim.Models;
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

        // İsim yerine ID kullanıyoruz, böylece Türkçe karakter sorunu tamamen ortadan kalkıyor
        [Route("Haberler")]
        [Route("Haberler/Kategori/{kategoriId:int}")]
        public async Task<IActionResult> Index(int? kategoriId, string arama, int sayfa = 1)
        {
            int pageSize = 6;
            var sorgu = _context.Haberlers.Include(h => h.Kategori).AsQueryable();

            // 1. Arama Durumu
            bool isAramaYapildi = !string.IsNullOrEmpty(arama);
            if (isAramaYapildi)
            {
                sorgu = sorgu.Where(h => h.Baslik.Contains(arama) || h.Ozet.Contains(arama));
                ViewBag.AramaYok = false;
            }
            else
            {
                ViewBag.AramaYok = true;
            }

            // 2. Kategori Durumu ve Başlık
            string sayfaBasligi = "Haberler ve Duyurular";
            if (kategoriId.HasValue)
            {
                sorgu = sorgu.Where(h => h.KategoriId == kategoriId.Value);
                var seciliKategori = await _context.HaberKategorileris.FindAsync(kategoriId.Value);
                if (seciliKategori != null)
                {
                    sayfaBasligi = seciliKategori.KategoriAdi; // Sayfa başlığı "Basın" vs. olacak
                }
            }

            // Eğer sorgu URL içindeki Query String (?arama=... veya ?kategoriId=...) ile yapıldıysa filtre yazısını göster.
            // Ama navbar'dan Route (/Haberler/Kategori/1) ile tıklandıysa GÖSTERME!
            ViewBag.FiltreUygulandi = isAramaYapildi || HttpContext.Request.Query.ContainsKey("kategoriId");
            ViewBag.SayfaBasligi = sayfaBasligi;

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

            ViewBag.AramaKelimesi = arama;
            ViewBag.SeciliKategori = kategoriId;
            ViewBag.ToplamKayit = toplamKayit;
            ViewBag.MevcutSayfa = sayfa;
            ViewBag.ToplamSayfa = (int)Math.Ceiling((double)toplamKayit / pageSize);

            // Dropdown listeleri
            var kategoriler = await _context.HaberKategorileris.ToListAsync();
            ViewBag.Kategoriler = kategoriler;
            ViewBag.KategoriListesi = new SelectList(kategoriler, "Id", "KategoriAdi");

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
        public async Task<IActionResult> HaberKaydet(Haberler model, IFormFile Fotograf)
        {
            if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Index");

            try
            {
                if (Fotograf != null && Fotograf.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Fotograf.FileName);
                    string path = Path.Combine(_env.WebRootPath, "uploads/haberler", fileName);

                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "uploads/haberler"));

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await Fotograf.CopyToAsync(stream);
                    }
                    model.FotografYolu = "/uploads/haberler/" + fileName;
                }

                model.YayimTarihi = DateTime.Now;
                _context.Add(model);
                await _context.SaveChangesAsync();

                TempData["Basari"] = "Haber başarıyla eklendi.";
            }
            catch (Exception)
            {
                TempData["Hata"] = "Haber eklenirken bir sorun oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HaberGuncelle(Haberler model, IFormFile Fotograf)
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

                if (Fotograf != null && Fotograf.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Fotograf.FileName);
                    string path = Path.Combine(_env.WebRootPath, "uploads/haberler", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await Fotograf.CopyToAsync(stream);
                    }
                    mevcutHaber.FotografYolu = "/uploads/haberler/" + fileName;
                }

                _context.Update(mevcutHaber);
                await _context.SaveChangesAsync();

                TempData["Basari"] = "Haber başarıyla güncellendi.";
            }
            catch (Exception)
            {
                TempData["Hata"] = "Güncelleme sırasında bir hata oluştu.";
            }

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