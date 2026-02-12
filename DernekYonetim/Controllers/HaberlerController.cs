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

    // Haberleri Listele
    public async Task<IActionResult> Index()
    {
        // Haberleri kategorileriyle beraber çekiyoruz
        var haberler = await _context.Haberlers.Include(h => h.Kategori).ToListAsync();

        // KATEGORİLERİ BURADA ÇEKİYORUZ
        // SelectList oluştururken kolon isimlerinin DB ile birebir aynı olması şart.
        // Senin modelinde: "Id" ve "KategoriAdi"
        var kategoriler = _context.HaberKategorileris.ToList();

        // Dropdown için ViewBag'e gönderiyoruz
        ViewBag.KategoriListesi = new SelectList(kategoriler, "Id", "KategoriAdi");

        return View(haberler);
    }

    [HttpPost]
    public async Task<IActionResult> HaberKaydet(Haberler haber, IFormFile? Fotograf)
    {
        // 1. Dosya işlemi
        if (Fotograf != null && Fotograf.Length > 0)
        {
            string dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(Fotograf.FileName);
            string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "haberler");

            if (!Directory.Exists(yol)) Directory.CreateDirectory(yol);

            string tamYol = Path.Combine(yol, dosyaAdi);
            using (var stream = new FileStream(tamYol, FileMode.Create))
            {
                await Fotograf.CopyToAsync(stream);
            }
            haber.FotografYolu = "/img/haberler/" + dosyaAdi;
        }

        // 2. Zorunlu alanları elle doldur (Modeldeki property isimlerinle birebir aynı)
        haber.YayimTarihi = DateTime.Now;

        // Veritabanında boş olamaz hatası almamak için geçici bir tarih
        if (haber.BitisTarihi == null) haber.BitisTarihi = DateTime.Now.AddMonths(1);

        // 3. Veritabanına Ekleme
        _context.Haberlers.Add(haber);
        await _context.SaveChangesAsync();

        // 4. Sayfayı yenile
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> HaberGuncelle(Haberler gelenHaber, IFormFile? Fotograf)
    {
        // DB'deki mevcut kaydı bulalım
        var mevcutHaber = await _context.Haberlers.FindAsync(gelenHaber.Id);
        if (mevcutHaber == null) return NotFound();

        // Verileri güncelleyelim
        mevcutHaber.Baslik = gelenHaber.Baslik;
        mevcutHaber.Ozet = gelenHaber.Ozet;
        mevcutHaber.Icerik = gelenHaber.Icerik;
        mevcutHaber.KategoriId = gelenHaber.KategoriId;

        // Fotoğraf yüklenmişse eskisini silip yenisini kaydedelim
        if (Fotograf != null && Fotograf.Length > 0)
        {
            // Eski fotoğrafı sil (fiziksel dosya)
            if (!string.IsNullOrEmpty(mevcutHaber.FotografYolu))
            {
                var eskiYol = Path.Combine(_hostEnvironment.WebRootPath, mevcutHaber.FotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(eskiYol)) System.IO.File.Delete(eskiYol);
            }

            // Yeni fotoğrafı kaydet
            string dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(Fotograf.FileName);
            string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "haberler", dosyaAdi);

            using (var stream = new FileStream(yol, FileMode.Create))
            {
                await Fotograf.CopyToAsync(stream);
            }
            mevcutHaber.FotografYolu = "/img/haberler/" + dosyaAdi;
        }

        _context.Haberlers.Update(mevcutHaber);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}