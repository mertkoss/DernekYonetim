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

    public async Task<IActionResult> Index(string arama, int sayfa = 1)
    {
        int sayfaBoyutu = 6;

        var query = _context.Galeris.AsQueryable();

        // GÜNCELLENEN KISIM: Sadece "Baslik" alanında arama yapıyoruz
        if (!string.IsNullOrEmpty(arama))
        {
            query = query.Where(x => x.Baslik.Contains(arama));
        }

        var toplamKayit = await query.CountAsync();
        var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu);

        var liste = await query.OrderByDescending(x => x.YuklemeTarihi)
                               .Skip((sayfa - 1) * sayfaBoyutu)
                               .Take(sayfaBoyutu)
                               .ToListAsync();

        var model = new HomeViewModel { Galeri = liste };

        ViewBag.MevcutSayfa = sayfa;
        ViewBag.ToplamSayfa = toplamSayfa;
        ViewBag.AramaKelimesi = arama;
        ViewBag.ToplamKayit = toplamKayit;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Ekle(string Baslik, string Aciklama, IFormFile Fotograf)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (Fotograf != null && Fotograf.Length > 0)
        {
            string dosyaUzantisi = Path.GetExtension(Fotograf.FileName);
            string yeniDosyaAdi = Guid.NewGuid().ToString() + dosyaUzantisi;

            string yuklemeYolu = Path.Combine(_hostEnvironment.WebRootPath, "img", "galeri");

            if (!Directory.Exists(yuklemeYolu))
                Directory.CreateDirectory(yuklemeYolu);

            string tamYol = Path.Combine(yuklemeYolu, yeniDosyaAdi);

            using (var stream = new FileStream(tamYol, FileMode.Create))
            {
                await Fotograf.CopyToAsync(stream);
            }

            var yeniGaleri = new Galeri
            {
                Baslik = Baslik,
                Aciklama = Aciklama,
                FotografYolu = "/img/galeri/" + yeniDosyaAdi,
                YuklemeTarihi = DateTime.Now
            };

            _context.Galeris.Add(yeniGaleri);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        return BadRequest("Dosya yüklenemedi.");
    }

    public async Task<IActionResult> Sil(int id)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var foto = await _context.Galeris.FindAsync(id);
        if (foto != null)
        {
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
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var mevcutKayit = await _context.Galeris.FindAsync(Id);
        if (mevcutKayit == null) return NotFound();

        mevcutKayit.Baslik = Baslik;
        mevcutKayit.Aciklama = Aciklama;

        if (Fotograf != null && Fotograf.Length > 0)
        {
            if (!string.IsNullOrEmpty(mevcutKayit.FotografYolu))
            {
                var eskiDosyaYolu = Path.Combine(_hostEnvironment.WebRootPath, mevcutKayit.FotografYolu.TrimStart('/'));
                if (System.IO.File.Exists(eskiDosyaYolu)) System.IO.File.Delete(eskiDosyaYolu);
            }

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