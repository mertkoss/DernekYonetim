using Microsoft.AspNetCore.Mvc;
using DernekYonetim.Models; // Modelinizin olduğu namespace
using System.IO;

public class GaleriController : Controller
{
    private readonly DernekYonetimContext _context; // Veritabanı context isminizi buraya yazın
    private readonly IWebHostEnvironment _hostEnvironment;

    public GaleriController(DernekYonetimContext context, IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    // Galeri Listeleme
    public IActionResult Index()
    {
        var galeriListesi = _context.Galeris.OrderByDescending(x => x.YuklemeTarihi).ToList();
        var model = new HomeViewModel { Galeri = galeriListesi };
        return View(model);
    }

    // FOTOĞRAF EKLEME (POST)
    [HttpPost]
    public async Task<IActionResult> Ekle(string Baslik, string Aciklama, IFormFile Fotograf)
    {
        if (Fotograf != null && Fotograf.Length > 0)
        {
            // 1. Dosya adını benzersiz yapalım
            string dosyaUzantisi = Path.GetExtension(Fotograf.FileName);
            string yeniDosyaAdi = Guid.NewGuid().ToString() + dosyaUzantisi;

            // 2. Klasör yolunu belirleyelim (wwwroot/img/galeri)
            string yuklemeYolu = Path.Combine(_hostEnvironment.WebRootPath, "img", "galeri");

            if (!Directory.Exists(yuklemeYolu))
                Directory.CreateDirectory(yuklemeYolu);

            string tamYol = Path.Combine(yuklemeYolu, yeniDosyaAdi);

            // 3. Dosyayı klasöre kaydedelim
            using (var stream = new FileStream(tamYol, FileMode.Create))
            {
                await Fotograf.CopyToAsync(stream);
            }

            // 4. Veritabanına kaydedelim
            var yeniGaleri = new Galeri
            {
                Baslik = Baslik,
                Aciklama = Aciklama,
                FotografYolu = "/img/galeri/" + yeniDosyaAdi, // Web üzerinden erişilecek yol
                YuklemeTarihi = DateTime.Now
            };

            _context.Galeris.Add(yeniGaleri);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index"); // İşlem bitince galeriye dön
        }

        return BadRequest("Dosya yüklenemedi.");
    }

    // FOTOĞRAF SİLME
    public async Task<IActionResult> Sil(int id)
    {
        var foto = await _context.Galeris.FindAsync(id);
        if (foto != null)
        {
            // Fiziksel dosyayı da silelim (Opsiyonel ama tavsiye edilir)
            var dosyaYolu = Path.Combine(_hostEnvironment.WebRootPath, foto.FotografYolu.TrimStart('/'));
            if (System.IO.File.Exists(dosyaYolu))
                System.IO.File.Delete(dosyaYolu);

            _context.Galeris.Remove(foto);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Guncelle(int Id, string Baslik, string Aciklama, IFormFile? Fotograf)
    {
        var mevcutKayit = await _context.Galeris.FindAsync(Id);
        if (mevcutKayit == null) return NotFound();

        mevcutKayit.Baslik = Baslik;
        mevcutKayit.Aciklama = Aciklama;

        // Eğer yeni bir fotoğraf dosyası seçildiyse:
        if (Fotograf != null && Fotograf.Length > 0)
        {
            // 1. Eski dosyayı fiziksel olarak sil (yer kaplamasın)
            if (!string.IsNullOrEmpty(mevcutKayit.FotografYolu))
            {
                var eskiDosyaYolu = Path.Combine(_hostEnvironment.WebRootPath, mevcutKayit.FotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(eskiDosyaYolu)) System.IO.File.Delete(eskiDosyaYolu);
            }

            // 2. Yeni dosyayı kaydet
            string yeniDosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(Fotograf.FileName);
            string tamYol = Path.Combine(_hostEnvironment.WebRootPath, "img", "galeri", yeniDosyaAdi);

            using (var stream = new FileStream(tamYol, FileMode.Create))
            {
                await Fotograf.CopyToAsync(stream);
            }

            mevcutKayit.FotografYolu = "/img/galeri/" + yeniDosyaAdi;
        }

        _context.Galeris.Update(mevcutKayit);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}