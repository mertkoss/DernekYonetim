using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DernekYonetim.Models;
using System.IO;

public class KayiplarController : Controller
{
    private readonly DernekYonetimContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;

    public KayiplarController(DernekYonetimContext context, IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<IActionResult> Index(string arama, int sayfa = 1)
    {
        int sayfaBoyutu = 5;
        var query = _context.Kaybettiklerimizs.AsQueryable();

        if (!string.IsNullOrEmpty(arama))
        {
            query = query.Where(x => x.AdSoyad.Contains(arama) || x.Aciklama.Contains(arama));
        }

        var toplamKayit = await query.CountAsync();
        var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu);

        var liste = await query.OrderByDescending(x => x.VefatTarihi)
                               .Skip((sayfa - 1) * sayfaBoyutu)
                               .Take(sayfaBoyutu)
                               .ToListAsync();

        ViewBag.MevcutSayfa = sayfa;
        ViewBag.ToplamSayfa = toplamSayfa;
        ViewBag.AramaKelimesi = arama;
        ViewBag.ToplamKayit = toplamKayit;

        // AJAX ile arama gelirse Partial dön
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_KayiplarListesiPartial", liste);
        }

        return View(liste);
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // GÜVENLİK
    public async Task<IActionResult> KayitEkle(Kaybettiklerimiz model, IFormFile? Fotograf, DateTime VefatTarihiInput, DateTime? DogumTarihiInput)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        if (string.IsNullOrWhiteSpace(model.AdSoyad))
        {
            TempData["Hata"] = "Ad Soyad alanı zorunludur.";
            return RedirectToAction(nameof(Index));
        }

        if (DogumTarihiInput.HasValue && VefatTarihiInput < DogumTarihiInput.Value)
        {
            TempData["Hata"] = "Vefat tarihi, doğum tarihinden önce olamaz.";
            return RedirectToAction(nameof(Index));
        }

        if (Fotograf != null && Fotograf.Length > 0)
        {
            // FOTOĞRAF GÜVENLİK KONTROLLERİ
            if (Fotograf.Length > 3 * 1024 * 1024)
            {
                TempData["Hata"] = "Fotoğraf boyutu en fazla 3 MB olmalıdır.";
                return RedirectToAction("Index");
            }

            var izinVerilenUzantilar = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var uzanti = Path.GetExtension(Fotograf.FileName).ToLowerInvariant();
            if (!izinVerilenUzantilar.Contains(uzanti))
            {
                TempData["Hata"] = "Sadece resim dosyası yükleyebilirsiniz.";
                return RedirectToAction("Index");
            }

            string dosyaAdi = Guid.NewGuid().ToString() + uzanti;
            string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "kayiplar");
            if (!Directory.Exists(yol)) Directory.CreateDirectory(yol);

            using (var stream = new FileStream(Path.Combine(yol, dosyaAdi), FileMode.Create))
            {
                await Fotograf.CopyToAsync(stream);
            }
            model.FotografYolu = "/img/kayiplar/" + dosyaAdi;
        }

        model.VefatTarihi = DateOnly.FromDateTime(VefatTarihiInput);
        if (DogumTarihiInput.HasValue)
        {
            model.DogumTarihi = DateOnly.FromDateTime(DogumTarihiInput.Value);
        }

        _context.Kaybettiklerimizs.Add(model);
        await _context.SaveChangesAsync();

        TempData["Basari"] = "Kayıt başarıyla eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // GÜVENLİK (POST'a ÇEVRİLDİ)
    public async Task<IActionResult> Sil(int id)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        var kayip = await _context.Kaybettiklerimizs.FindAsync(id);
        if (kayip != null)
        {
            // ESKİ DOSYAYI FİZİKSEL OLARAK SİL
            if (!string.IsNullOrEmpty(kayip.FotografYolu))
            {
                var dosyaYolu = Path.Combine(_hostEnvironment.WebRootPath, kayip.FotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(dosyaYolu)) System.IO.File.Delete(dosyaYolu);
            }

            _context.Kaybettiklerimizs.Remove(kayip);
            await _context.SaveChangesAsync();
            TempData["Basari"] = "Kayıt başarıyla silindi.";
        }
        else
        {
            TempData["Hata"] = "Silinecek kayıt bulunamadı.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // GÜVENLİK
    public async Task<IActionResult> KayitGuncelle(Kaybettiklerimiz gelenVeri, IFormFile? Fotograf, DateTime VefatTarihiInput, DateTime? DogumTarihiInput)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        if (string.IsNullOrWhiteSpace(gelenVeri.AdSoyad))
        {
            TempData["Hata"] = "Ad Soyad alanı zorunludur.";
            return RedirectToAction(nameof(Index));
        }

        if (DogumTarihiInput.HasValue && VefatTarihiInput < DogumTarihiInput.Value)
        {
            TempData["Hata"] = "Vefat tarihi, doğum tarihinden önce olamaz.";
            return RedirectToAction(nameof(Index));
        }

        var mevcut = await _context.Kaybettiklerimizs.FindAsync(gelenVeri.Id);
        if (mevcut == null) return NotFound();

        mevcut.AdSoyad = gelenVeri.AdSoyad;
        mevcut.Aciklama = gelenVeri.Aciklama;
        mevcut.VefatTarihi = DateOnly.FromDateTime(VefatTarihiInput);
        if (DogumTarihiInput.HasValue)
        {
            mevcut.DogumTarihi = DateOnly.FromDateTime(DogumTarihiInput.Value);
        }

        if (Fotograf != null && Fotograf.Length > 0)
        {
            // FOTOĞRAF GÜVENLİK KONTROLLERİ
            if (Fotograf.Length > 3 * 1024 * 1024)
            {
                TempData["Hata"] = "Fotoğraf boyutu en fazla 3 MB olmalıdır.";
                return RedirectToAction("Index");
            }

            var izinVerilenUzantilar = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var uzanti = Path.GetExtension(Fotograf.FileName).ToLowerInvariant();
            if (!izinVerilenUzantilar.Contains(uzanti))
            {
                TempData["Hata"] = "Sadece resim dosyası yükleyebilirsiniz.";
                return RedirectToAction("Index");
            }

            // ESKİ FOTOĞRAFI DİSKTEN SİL (Sunucu şişmesin diye)
            if (!string.IsNullOrEmpty(mevcut.FotografYolu))
            {
                var eskiYol = Path.Combine(_hostEnvironment.WebRootPath, mevcut.FotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(eskiYol)) System.IO.File.Delete(eskiYol);
            }

            string dosyaAdi = Guid.NewGuid().ToString() + uzanti;
            string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "kayiplar");
            if (!Directory.Exists(yol)) Directory.CreateDirectory(yol);

            using (var stream = new FileStream(Path.Combine(yol, dosyaAdi), FileMode.Create))
            {
                await Fotograf.CopyToAsync(stream);
            }
            mevcut.FotografYolu = "/img/kayiplar/" + dosyaAdi;
        }

        _context.Kaybettiklerimizs.Update(mevcut);
        await _context.SaveChangesAsync();

        TempData["Basari"] = "Kayıt başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }
}