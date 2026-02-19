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
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }
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
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }

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
    // HABER DETAY SAYFASI
    public async Task<IActionResult> Detay(int id)
    {
        // Haberi kategorisiyle birlikte çekiyoruz
        var haber = await _context.Haberlers
            .Include(h => h.Kategori)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (haber == null)
        {
            return NotFound(); // Haber bulunamazsa 404 sayfasına atar
        }

        return View(haber);
    }
    // HABER SİLME İŞLEMİ
    public async Task<IActionResult> Sil(int id)
    {
        // 1. Yetki Kontrolü (Sadece Adminler Silebilir)
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        // 2. Veritabanından silinecek haberi bul
        var silinecekHaber = await _context.Haberlers.FindAsync(id);

        if (silinecekHaber != null)
        {
            // 3. Haberin sunucudaki fiziksel fotoğrafını da sil (Gereksiz yer kaplamasın)
            if (!string.IsNullOrEmpty(silinecekHaber.FotografYolu))
            {
                var dosyaYolu = Path.Combine(_hostEnvironment.WebRootPath, silinecekHaber.FotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(dosyaYolu))
                {
                    System.IO.File.Delete(dosyaYolu);
                }
            }

            // 4. Haberi veritabanından tamamen kaldır
            _context.Haberlers.Remove(silinecekHaber);
            await _context.SaveChangesAsync();
        }

        // 5. Silme işlemi bitince ana haberler sayfasına geri yönlendir
        return RedirectToAction("Index");
    }
}