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
        if (upload != null && upload.Length > 0)
        {
            // 1. Resim için benzersiz bir isim oluştur (Çakışma olmasın diye)
            var dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(upload.FileName);

            // 2. Yüklenecek klasörü belirle (wwwroot/img/icerik)
            var klasorYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/icerik");
            if (!Directory.Exists(klasorYolu)) Directory.CreateDirectory(klasorYolu);

            var tamYol = Path.Combine(klasorYolu, dosyaAdi);

            // 3. Dosyayı sunucuya kaydet
            using (var stream = new FileStream(tamYol, FileMode.Create))
            {
                await upload.CopyToAsync(stream);
            }

            // 4. CKEditor'ün anlayacağı formatta (JSON) resmin URL'sini geri döndür
            var url = $"/img/icerik/{dosyaAdi}";
            return Json(new { uploaded = 1, fileName = dosyaAdi, url = url });
        }

        return Json(new { uploaded = 0, error = new { message = "Resim yüklenemedi." } });
    }
}