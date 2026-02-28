using DernekYonetim.ViewModels;
using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class DernekController : Controller
{
    private readonly DernekYonetimContext _context;

    public DernekController(DernekYonetimContext context)
    {
        _context = context;
    }

    // Görüntüleme Aksiyonu
    [Route("Dernek/Hakkinda/{slug}")]
    public IActionResult Hakkinda(string slug)
    {
        var bolum = _context.DernekHakkindaBolumleris
            .FirstOrDefault(x => x.Slug == slug && x.Aktif);

        if (bolum == null)
            return NotFound();

        var model = new HomeViewModel
        {
            About = bolum
        };

        return View(model);
    }

    // Güncelleme Aksiyonu (Modal'dan gelen veriler için)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BolumGuncelle(DernekHakkindaBolumleri model)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }
        var mevcutBolum = await _context.DernekHakkindaBolumleris.FindAsync(model.Id);

        if (mevcutBolum == null)
            return NotFound();

        // Alanları güncelle
        mevcutBolum.Baslik = model.Baslik;
        mevcutBolum.Icerik = model.Icerik;
        mevcutBolum.Sira = model.Sira;
        mevcutBolum.Aktif = model.Aktif;
        mevcutBolum.GuncellemeTarihi = DateTime.Now;

        _context.DernekHakkindaBolumleris.Update(mevcutBolum);
        await _context.SaveChangesAsync();

        // İşlem bitince güncellenen sayfanın slug'ına geri yönlendir
        return RedirectToAction("Hakkinda", new { slug = mevcutBolum.Slug });
    }

    // CKEDITOR BİLGİSAYARDAN RESİM YÜKLEME METODU
    [HttpPost]
    public async Task<IActionResult> ResimYukle(IFormFile upload)
    {
        // 1. GÜVENLİK: Sadece giriş yapan adminler dosya yükleyebilir
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return Json(new { uploaded = 0, error = new { message = "Yetkisiz işlem!" } });
        }

        if (upload != null && upload.Length > 0)
        {
            // 2. GÜVENLİK: Sadece resim formatlarına izin ver
            var izinVerilenUzantilar = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var uzanti = Path.GetExtension(upload.FileName).ToLowerInvariant();

            if (!izinVerilenUzantilar.Contains(uzanti))
            {
                return Json(new { uploaded = 0, error = new { message = "Sadece görsel (.jpg, .png, vb.) yükleyebilirsiniz!" } });
            }

            var dosyaAdi = Guid.NewGuid().ToString() + uzanti;
            var klasorYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/icerik");

            if (!Directory.Exists(klasorYolu)) Directory.CreateDirectory(klasorYolu);
            var tamYol = Path.Combine(klasorYolu, dosyaAdi);

            using (var stream = new FileStream(tamYol, FileMode.Create))
            {
                await upload.CopyToAsync(stream);
            }

            var url = $"/img/icerik/{dosyaAdi}";
            return Json(new { uploaded = 1, fileName = dosyaAdi, url = url });
        }

        return Json(new { uploaded = 0, error = new { message = "Dosya boş olamaz." } });
    }
}