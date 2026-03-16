using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DernekYonetim.Models;
using System.IO;

public class GaleriController : Controller
{
    private readonly DernekYonetimContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;

    public GaleriController(DernekYonetimContext context, IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    // --- 1. ALBÜM (KLASÖR) İŞLEMLERİ ---

    public async Task<IActionResult> Index(string arama, int sayfa = 1)
    {
        int sayfaBoyutu = 6;
        var query = _context.GaleriAlbumleri.AsQueryable();

        if (!string.IsNullOrEmpty(arama))
        {
            query = query.Where(x => x.Baslik.Contains(arama));
        }

        var toplamKayit = await query.CountAsync();
        var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu);

        var liste = await query.Include(x => x.Fotograflar)
                               .OrderByDescending(x => x.OlusturulmaTarihi)
                               .Skip((sayfa - 1) * sayfaBoyutu)
                               .Take(sayfaBoyutu)
                               .ToListAsync();

        var model = new HomeViewModel { GaleriAlbumleri = liste };
        ViewBag.MevcutSayfa = sayfa; ViewBag.ToplamSayfa = toplamSayfa;
        ViewBag.AramaKelimesi = arama; ViewBag.ToplamKayit = toplamKayit;

        // EĞER İSTEK AJAX (Canlı Arama) İLE GELDİYSE SADECE PARTIAL DÖNDÜR
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_AlbumListesiPartial", model);
        }

        // NORMAL SAYFA YÜKLEMESİYSE TÜM SAYFAYI DÖNDÜR
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlbumEkle(string Baslik, string Aciklama, IFormFile KapakFotografi)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        var yeniAlbum = new GaleriAlbum { Baslik = Baslik, Aciklama = Aciklama, OlusturulmaTarihi = DateTime.Now };

        if (KapakFotografi != null && KapakFotografi.Length > 0)
        {
            string yeniAdi = Guid.NewGuid().ToString() + Path.GetExtension(KapakFotografi.FileName).ToLowerInvariant();
            string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "galeri", "albumler");
            if (!Directory.Exists(yol)) Directory.CreateDirectory(yol);

            using (var stream = new FileStream(Path.Combine(yol, yeniAdi), FileMode.Create))
            {
                await KapakFotografi.CopyToAsync(stream);
            }
            yeniAlbum.KapakFotografYolu = "/img/galeri/albumler/" + yeniAdi;
        }

        _context.GaleriAlbumleri.Add(yeniAlbum);
        await _context.SaveChangesAsync();
        TempData["Mesaj"] = "Albüm oluşturuldu."; TempData["Durum"] = "success";
        return RedirectToAction("Index");
    }

    // YENİ EKLENEN: Albüm Düzenleme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlbumDuzenle(int Id, string Baslik, string Aciklama, IFormFile? KapakFotografi)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        var album = await _context.GaleriAlbumleri.FindAsync(Id);
        if (album == null) return NotFound();

        album.Baslik = Baslik;
        album.Aciklama = Aciklama;

        if (KapakFotografi != null && KapakFotografi.Length > 0)
        {
            // Eski kapak fotoğrafı varsa fiziksel olarak sil
            if (!string.IsNullOrEmpty(album.KapakFotografYolu))
            {
                var eskiYol = Path.Combine(_hostEnvironment.WebRootPath, album.KapakFotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(eskiYol)) System.IO.File.Delete(eskiYol);
            }

            string yeniAdi = Guid.NewGuid().ToString() + Path.GetExtension(KapakFotografi.FileName).ToLowerInvariant();
            string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "galeri", "albumler");
            if (!Directory.Exists(yol)) Directory.CreateDirectory(yol);

            using (var stream = new FileStream(Path.Combine(yol, yeniAdi), FileMode.Create))
            {
                await KapakFotografi.CopyToAsync(stream);
            }
            album.KapakFotografYolu = "/img/galeri/albumler/" + yeniAdi;
        }

        _context.GaleriAlbumleri.Update(album);
        await _context.SaveChangesAsync();
        TempData["Mesaj"] = "Albüm güncellendi."; TempData["Durum"] = "success";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlbumSil(int id)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        var album = await _context.GaleriAlbumleri.Include(x => x.Fotograflar).FirstOrDefaultAsync(x => x.Id == id);
        if (album != null)
        {
            foreach (var foto in album.Fotograflar)
            {
                var fotoYol = Path.Combine(_hostEnvironment.WebRootPath, foto.FotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(fotoYol)) System.IO.File.Delete(fotoYol);
            }
            if (!string.IsNullOrEmpty(album.KapakFotografYolu))
            {
                var kapakYol = Path.Combine(_hostEnvironment.WebRootPath, album.KapakFotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(kapakYol)) System.IO.File.Delete(kapakYol);
            }
            _context.GaleriAlbumleri.Remove(album);
            await _context.SaveChangesAsync();
            TempData["Mesaj"] = "Albüm ve fotoğraflar silindi."; TempData["Durum"] = "success";
        }
        return RedirectToAction("Index");
    }

    // --- 2. FOTOĞRAF İŞLEMLERİ (ALBÜM İÇİ - DETAY) ---

    public async Task<IActionResult> Detay(int id)
    {
        var album = await _context.GaleriAlbumleri.Include(x => x.Fotograflar).FirstOrDefaultAsync(x => x.Id == id);
        if (album == null) return NotFound();
        album.Fotograflar = album.Fotograflar.OrderByDescending(f => f.YuklemeTarihi).ToList();
        return View(album);
    }

    // GÜNCELLENEN: Çoklu Fotoğraf Yükleme (List<IFormFile>)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FotografEkle(int AlbumId, List<IFormFile> Fotograflar)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        if (Fotograflar != null && Fotograflar.Count > 0)
        {
            string yuklemeYolu = Path.Combine(_hostEnvironment.WebRootPath, "img", "galeri", "fotograflar");
            if (!Directory.Exists(yuklemeYolu)) Directory.CreateDirectory(yuklemeYolu);

            int yuklenenSayi = 0;
            foreach (var foto in Fotograflar)
            {
                if (foto.Length > 0)
                {
                    string yeniDosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(foto.FileName).ToLowerInvariant();
                    using (var stream = new FileStream(Path.Combine(yuklemeYolu, yeniDosyaAdi), FileMode.Create))
                    {
                        await foto.CopyToAsync(stream);
                    }

                    _context.GaleriFotograflari.Add(new GaleriFotograf
                    {
                        AlbumId = AlbumId,
                        Aciklama = "", // Toplu yüklemede açıklama boş bırakılır, istenirse sonra tek tek eklenir
                        FotografYolu = "/img/galeri/fotograflar/" + yeniDosyaAdi,
                        YuklemeTarihi = DateTime.Now
                    });
                    yuklenenSayi++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Mesaj"] = $"{yuklenenSayi} adet fotoğraf başarıyla eklendi.";
            TempData["Durum"] = "success";
        }
        else
        {
            TempData["Mesaj"] = "Lütfen en az bir fotoğraf seçin."; TempData["Durum"] = "warning";
        }

        return RedirectToAction("Detay", new { id = AlbumId });
    }

    // YENİ EKLENEN: Özel bir fotoğrafa sonradan açıklama eklemek için
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FotografDuzenle(int Id, int AlbumId, string Aciklama)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        var foto = await _context.GaleriFotograflari.FindAsync(Id);
        if (foto != null)
        {
            foto.Aciklama = Aciklama;
            _context.GaleriFotograflari.Update(foto);
            await _context.SaveChangesAsync();
            TempData["Mesaj"] = "Fotoğraf açıklaması güncellendi."; TempData["Durum"] = "success";
        }
        return RedirectToAction("Detay", new { id = AlbumId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FotografSil(int id, int albumId)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        var foto = await _context.GaleriFotograflari.FindAsync(id);
        if (foto != null)
        {
            var dosyaYolu = Path.Combine(_hostEnvironment.WebRootPath, foto.FotografYolu.TrimStart('/'));
            if (System.IO.File.Exists(dosyaYolu)) System.IO.File.Delete(dosyaYolu);

            _context.GaleriFotograflari.Remove(foto);
            await _context.SaveChangesAsync();
            TempData["Mesaj"] = "Fotoğraf silindi."; TempData["Durum"] = "success";
        }
        return RedirectToAction("Detay", new { id = albumId });
    }
}