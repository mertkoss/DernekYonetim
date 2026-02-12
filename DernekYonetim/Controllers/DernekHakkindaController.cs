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
}