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

        try
        {
            // 1. BOŞLUK (SPACE) VE NULL KONTROLÜ
            if (string.IsNullOrWhiteSpace(haber.Baslik) || string.IsNullOrWhiteSpace(haber.Ozet))
            {
                TempData["Hata"] = "Başlık ve Özet alanları sadece boşluklardan oluşamaz veya boş bırakılamaz.";
                return RedirectToAction("Index");
            }

            // 2. KATEGORİ SEÇİM KONTROLÜ
            if (haber.KategoriId == null || haber.KategoriId <= 0)
            {
                TempData["Hata"] = "Lütfen geçerli bir kategori seçiniz.";
                return RedirectToAction("Index");
            }

            // 3. UZUNLUK KONTROLLERİ (Hem Özet hem Başlık için)
            if (haber.Baslik.Length > 150)
            {
                TempData["Hata"] = "Haber başlığı 150 karakterden uzun olamaz.";
                return RedirectToAction("Index");
            }

            if (haber.Ozet.Length > 255)
            {
                TempData["Hata"] = "Haber özeti 255 karakterden uzun olamaz.";
                return RedirectToAction("Index");
            }

            // 4. DOSYA GÜVENLİK KONTROLLERİ
            if (Fotograf != null && Fotograf.Length > 0)
            {
                // A. Boyut Kontrolü (Maksimum 3 MB - 3 * 1024 * 1024 byte)
                if (Fotograf.Length > 3 * 1024 * 1024)
                {
                    TempData["Hata"] = "Yüklediğiniz fotoğrafın boyutu 3 MB'ı geçemez.";
                    return RedirectToAction("Index");
                }

                // B. Uzantı Kontrolü (Sadece Resim Formatlarına İzin Ver)
                var izinVerilenUzantilar = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var uzanti = Path.GetExtension(Fotograf.FileName).ToLowerInvariant();

                if (!izinVerilenUzantilar.Contains(uzanti))
                {
                    TempData["Hata"] = "Sadece .jpg, .jpeg, .png veya .webp formatında görsel yükleyebilirsiniz.";
                    return RedirectToAction("Index");
                }

                // Her şey tamamsa dosyayı kaydet
                string dosyaAdi = Guid.NewGuid().ToString() + uzanti;
                string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "haberler");

                if (!Directory.Exists(yol)) Directory.CreateDirectory(yol);

                string tamYol = Path.Combine(yol, dosyaAdi);
                using (var stream = new FileStream(tamYol, FileMode.Create))
                {
                    await Fotograf.CopyToAsync(stream);
                }
                haber.FotografYolu = "/img/haberler/" + dosyaAdi;
            }

            // Zaman atamaları
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
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            // 1. BOŞLUK KONTROLÜ
            if (string.IsNullOrWhiteSpace(gelenHaber.Baslik) || string.IsNullOrWhiteSpace(gelenHaber.Ozet))
            {
                TempData["Hata"] = "Başlık ve Özet alanları boş bırakılamaz.";
                // Not: İstersen hata olunca Detay sayfasına döndürebilirsin: return RedirectToAction("Detay", new { id = gelenHaber.Id });
                return RedirectToAction("Index");
            }

            // 2. UZUNLUK KONTROLLERİ
            if (gelenHaber.Baslik.Length > 150)
            {
                TempData["Hata"] = "Haber başlığı 150 karakterden uzun olamaz.";
                return RedirectToAction("Index");
            }

            if (gelenHaber.Ozet.Length > 255)
            {
                TempData["Hata"] = "Haber özeti 255 karakterden uzun olamaz.";
                return RedirectToAction("Index");
            }

            var mevcutHaber = await _context.Haberlers.FindAsync(gelenHaber.Id);
            if (mevcutHaber == null)
            {
                TempData["Hata"] = "Güncellenmek istenen haber bulunamadı.";
                return RedirectToAction("Index");
            }

            // Verileri güncelle
            mevcutHaber.Baslik = gelenHaber.Baslik;
            mevcutHaber.Ozet = gelenHaber.Ozet;
            mevcutHaber.Icerik = gelenHaber.Icerik;
            mevcutHaber.KategoriId = gelenHaber.KategoriId;
            mevcutHaber.SlayttaGoster = gelenHaber.SlayttaGoster;

            // 3. DOSYA GÜVENLİK KONTROLLERİ
            if (Fotograf != null && Fotograf.Length > 0)
            {
                // A. Boyut Kontrolü (Maks 3 MB)
                if (Fotograf.Length > 3 * 1024 * 1024)
                {
                    TempData["Hata"] = "Yüklediğiniz fotoğrafın boyutu 3 MB'ı geçemez.";
                    return RedirectToAction("Index");
                }

                // B. Uzantı Kontrolü
                var izinVerilenUzantilar = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var uzanti = Path.GetExtension(Fotograf.FileName).ToLowerInvariant();

                if (!izinVerilenUzantilar.Contains(uzanti))
                {
                    TempData["Hata"] = "Sadece .jpg, .jpeg, .png veya .webp formatında görsel yükleyebilirsiniz.";
                    return RedirectToAction("Index");
                }

                // Eski fotoğrafı sil (fiziksel dosya)
                if (!string.IsNullOrEmpty(mevcutHaber.FotografYolu))
                {
                    var eskiYol = Path.Combine(_hostEnvironment.WebRootPath, mevcutHaber.FotografYolu.TrimStart('/'));
                    if (System.IO.File.Exists(eskiYol)) System.IO.File.Delete(eskiYol);
                }

                // Yeni fotoğrafı kaydet
                string dosyaAdi = Guid.NewGuid().ToString() + uzanti;
                string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "haberler", dosyaAdi);

                using (var stream = new FileStream(yol, FileMode.Create))
                {
                    await Fotograf.CopyToAsync(stream);
                }
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

        ViewBag.KategoriListesi = new SelectList(_context.HaberKategorileris.ToList(), "Id", "KategoriAdi");

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