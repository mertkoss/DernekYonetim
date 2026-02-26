using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DernekYonetim.Models;

public class HaberlerController : Controller
{
    private readonly DernekYonetimContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;

    public HaberlerController(DernekYonetimContext context, IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    // GÜNCELLENEN KISIM: Arama, Filtreleme ve Sayfalama
    public async Task<IActionResult> Index(string arama, int? kategoriId, int sayfa = 1)
    {
        // Sayfada gösterilecek haber sayısı (Dikey akış olduğu için 5 idealdir)
        int sayfaBoyutu = 5;
        var query = _context.Haberlers.Include(h => h.Kategori).AsQueryable();

        // 1. Arama Filtresi (Sadece Başlıkta)
        if (!string.IsNullOrEmpty(arama))
        {
            query = query.Where(x => x.Baslik.Contains(arama));
        }

        // 2. Kategori Filtresi (Örn: Sadece Duyurular)
        if (kategoriId.HasValue && kategoriId > 0)
        {
            query = query.Where(x => x.KategoriId == kategoriId);
        }

        // 3. Sayfalama Hesaplamaları
        var toplamKayit = await query.CountAsync();
        var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu);

        var liste = await query.OrderByDescending(x => x.YayimTarihi)
                               .Skip((sayfa - 1) * sayfaBoyutu)
                               .Take(sayfaBoyutu)
                               .ToListAsync();

        // 4. Slider İçin Haberleri Seçme (Sadece ilk sayfadaysa ve arama/filtre yoksa göster)
        bool aramaYok = string.IsNullOrEmpty(arama) && !kategoriId.HasValue && sayfa == 1;
        if (aramaYok)
        {
            // Önce yöneticinin "Slaytta Göster" dediklerini al
            var sliderHaberler = await _context.Haberlers.Include(h => h.Kategori)
                                            .Where(x => x.SlayttaGoster == true)
                                            .OrderByDescending(x => x.YayimTarihi)
                                            .Take(3).ToListAsync();

            // Eğer hiç seçili yoksa, genel en yeni 3 haberi al
            if (!sliderHaberler.Any())
            {
                sliderHaberler = await _context.Haberlers.Include(h => h.Kategori)
                                            .OrderByDescending(x => x.YayimTarihi)
                                            .Take(3).ToListAsync();
            }
            ViewBag.SliderHaberler = sliderHaberler;
        }
        else
        {
            ViewBag.SliderHaberler = new List<Haberler>(); // Arama yapılıyorsa slider'ı boşalt (gizle)
        }

        // Kategorileri Dropdown için View'a gönder
        var kategoriler = await _context.HaberKategorileris.ToListAsync();
        ViewBag.Kategoriler = kategoriler;
        ViewBag.KategoriListesi = new SelectList(kategoriler, "Id", "KategoriAdi");

        ViewBag.MevcutSayfa = sayfa;
        ViewBag.ToplamSayfa = toplamSayfa;
        ViewBag.AramaKelimesi = arama;
        ViewBag.SeciliKategori = kategoriId;
        ViewBag.ToplamKayit = toplamKayit;
        ViewBag.AramaYok = aramaYok;

        return View(liste);
    }

    [HttpPost]
    public async Task<IActionResult> HaberKaydet(Haberler haber, IFormFile? Fotograf)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        try
        {
            if (string.IsNullOrWhiteSpace(haber.Baslik) || string.IsNullOrWhiteSpace(haber.Ozet))
            {
                TempData["Hata"] = "Başlık ve Özet alanları boş bırakılamaz.";
                return RedirectToAction("Index");
            }

            if (haber.KategoriId == null || haber.KategoriId <= 0)
            {
                TempData["Hata"] = "Lütfen geçerli bir kategori seçiniz.";
                return RedirectToAction("Index");
            }

            if (haber.Baslik.Length > 150) { TempData["Hata"] = "Haber başlığı 150 karakterden uzun olamaz."; return RedirectToAction("Index"); }
            if (haber.Ozet.Length > 255) { TempData["Hata"] = "Haber özeti 255 karakterden uzun olamaz."; return RedirectToAction("Index"); }

            if (Fotograf != null && Fotograf.Length > 0)
            {
                if (Fotograf.Length > 3 * 1024 * 1024) { TempData["Hata"] = "Yüklediğiniz fotoğrafın boyutu 3 MB'ı geçemez."; return RedirectToAction("Index"); }
                var izinVerilenUzantilar = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var uzanti = Path.GetExtension(Fotograf.FileName).ToLowerInvariant();
                if (!izinVerilenUzantilar.Contains(uzanti)) { TempData["Hata"] = "Sadece .jpg, .jpeg, .png veya .webp formatında görsel yükleyebilirsiniz."; return RedirectToAction("Index"); }

                string dosyaAdi = Guid.NewGuid().ToString() + uzanti;
                string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "haberler");
                if (!Directory.Exists(yol)) Directory.CreateDirectory(yol);
                string tamYol = Path.Combine(yol, dosyaAdi);
                using (var stream = new FileStream(tamYol, FileMode.Create)) { await Fotograf.CopyToAsync(stream); }
                haber.FotografYolu = "/img/haberler/" + dosyaAdi;
            }

            haber.YayimTarihi = DateTime.Now;
            if (haber.BitisTarihi == null) haber.BitisTarihi = DateTime.Now.AddMonths(1);

            _context.Haberlers.Add(haber);
            await _context.SaveChangesAsync();
            TempData["Basari"] = "Haber başarıyla yayınlandı.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["Hata"] = "Haber kaydedilirken sistemsel bir sorun oluştu: " + ex.Message;
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> HaberGuncelle(Haberler gelenHaber, IFormFile? Fotograf)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        try
        {
            if (string.IsNullOrWhiteSpace(gelenHaber.Baslik) || string.IsNullOrWhiteSpace(gelenHaber.Ozet)) { TempData["Hata"] = "Başlık ve Özet alanları boş bırakılamaz."; return RedirectToAction("Index"); }
            if (gelenHaber.Baslik.Length > 150) { TempData["Hata"] = "Haber başlığı 150 karakterden uzun olamaz."; return RedirectToAction("Index"); }
            if (gelenHaber.Ozet.Length > 255) { TempData["Hata"] = "Haber özeti 255 karakterden uzun olamaz."; return RedirectToAction("Index"); }

            var mevcutHaber = await _context.Haberlers.FindAsync(gelenHaber.Id);
            if (mevcutHaber == null) { TempData["Hata"] = "Güncellenmek istenen haber bulunamadı."; return RedirectToAction("Index"); }

            mevcutHaber.Baslik = gelenHaber.Baslik;
            mevcutHaber.Ozet = gelenHaber.Ozet;
            mevcutHaber.Icerik = gelenHaber.Icerik;
            mevcutHaber.KategoriId = gelenHaber.KategoriId;
            mevcutHaber.SlayttaGoster = gelenHaber.SlayttaGoster;

            if (Fotograf != null && Fotograf.Length > 0)
            {
                if (Fotograf.Length > 3 * 1024 * 1024) { TempData["Hata"] = "Yüklediğiniz fotoğrafın boyutu 3 MB'ı geçemez."; return RedirectToAction("Index"); }
                var izinVerilenUzantilar = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var uzanti = Path.GetExtension(Fotograf.FileName).ToLowerInvariant();
                if (!izinVerilenUzantilar.Contains(uzanti)) { TempData["Hata"] = "Sadece .jpg, .jpeg, .png veya .webp formatında görsel yükleyebilirsiniz."; return RedirectToAction("Index"); }

                if (!string.IsNullOrEmpty(mevcutHaber.FotografYolu))
                {
                    var eskiYol = Path.Combine(_hostEnvironment.WebRootPath, mevcutHaber.FotografYolu.TrimStart('/'));
                    if (System.IO.File.Exists(eskiYol)) System.IO.File.Delete(eskiYol);
                }

                string dosyaAdi = Guid.NewGuid().ToString() + uzanti;
                string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "haberler", dosyaAdi);
                using (var stream = new FileStream(yol, FileMode.Create)) { await Fotograf.CopyToAsync(stream); }
                mevcutHaber.FotografYolu = "/img/haberler/" + dosyaAdi;
            }

            _context.Haberlers.Update(mevcutHaber);
            await _context.SaveChangesAsync();
            TempData["Basari"] = "Haber başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            TempData["Hata"] = "Haber güncellenirken beklenmedik bir hata oluştu.";
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> Detay(int id)
    {
        var haber = await _context.Haberlers.Include(h => h.Kategori).FirstOrDefaultAsync(h => h.Id == id);
        if (haber == null) return NotFound();
        ViewBag.KategoriListesi = new SelectList(_context.HaberKategorileris.ToList(), "Id", "KategoriAdi");
        return View(haber);
    }

    public async Task<IActionResult> Sil(int id)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");
        var silinecekHaber = await _context.Haberlers.FindAsync(id);

        if (silinecekHaber != null)
        {
            if (!string.IsNullOrEmpty(silinecekHaber.FotografYolu))
            {
                var dosyaYolu = Path.Combine(_hostEnvironment.WebRootPath, silinecekHaber.FotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(dosyaYolu)) System.IO.File.Delete(dosyaYolu);
            }
            _context.Haberlers.Remove(silinecekHaber);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}