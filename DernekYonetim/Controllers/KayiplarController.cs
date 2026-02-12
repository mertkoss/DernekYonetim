using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DernekYonetim.Models;

public class KayiplarController : Controller
{
    private readonly DernekYonetimContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;

    public KayiplarController(DernekYonetimContext context, IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<IActionResult> Index()
    {
        // Vefat tarihine göre en yeniden en eskiye sıralayalım
        var liste = await _context.Kaybettiklerimizs.OrderByDescending(x => x.VefatTarihi).ToListAsync();
        return View(liste);
    }

    [HttpPost]
    public async Task<IActionResult> KayitEkle(Kaybettiklerimiz model, IFormFile? Fotograf, DateTime VefatTarihiInput)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }
        if (Fotograf != null && Fotograf.Length > 0)
        {
            string dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(Fotograf.FileName);
            string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "kayiplar");
            if (!Directory.Exists(yol)) Directory.CreateDirectory(yol);

            using (var stream = new FileStream(Path.Combine(yol, dosyaAdi), FileMode.Create))
            {
                await Fotograf.CopyToAsync(stream);
            }
            model.FotografYolu = "/img/kayiplar/" + dosyaAdi;
        }

        // DateOnly dönüşümü (Kritik nokta)
        model.VefatTarihi = DateOnly.FromDateTime(VefatTarihiInput);

        _context.Kaybettiklerimizs.Add(model);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Sil(int id)
    {
        var kayip = await _context.Kaybettiklerimizs.FindAsync(id);
        if (kayip != null)
        {
            _context.Kaybettiklerimizs.Remove(kayip);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> KayitGuncelle(Kaybettiklerimiz gelenVeri, IFormFile? Fotograf, DateTime VefatTarihiInput)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var mevcut = await _context.Kaybettiklerimizs.FindAsync(gelenVeri.Id);
        if (mevcut == null) return NotFound();

        mevcut.AdSoyad = gelenVeri.AdSoyad;
        mevcut.Aciklama = gelenVeri.Aciklama;
        mevcut.VefatTarihi = DateOnly.FromDateTime(VefatTarihiInput);

        if (Fotograf != null && Fotograf.Length > 0)
        {
            // Eski fotoğrafı silme (isteğe bağlı)
            string dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(Fotograf.FileName);
            string yol = Path.Combine(_hostEnvironment.WebRootPath, "img", "kayiplar", dosyaAdi);
            using (var stream = new FileStream(yol, FileMode.Create))
            {
                await Fotograf.CopyToAsync(stream);
            }
            mevcut.FotografYolu = "/img/kayiplar/" + dosyaAdi;
        }

        _context.Kaybettiklerimizs.Update(mevcut);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}