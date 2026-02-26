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

    // GÜNCELLENEN KISIM: Arama ve Sayfalama (Pagination) işlemleri buraya eklendi
    public async Task<IActionResult> Index(string arama, int sayfa = 1)
    {
        // Sayfada kaç adet kayıt gösterileceğini buradan ayarlayabilirsin
        int sayfaBoyutu = 5;

        // Temel sorguyu oluştur
        var query = _context.Kaybettiklerimizs.AsQueryable();

        // Eğer arama kutusuna bir şey yazılmışsa filtrele
        if (!string.IsNullOrEmpty(arama))
        {
            query = query.Where(x => x.AdSoyad.Contains(arama) || x.Aciklama.Contains(arama));
        }

        // Toplam kayıt ve sayfa sayısını hesapla
        var toplamKayit = await query.CountAsync();
        var toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu);

        // Vefat tarihine göre sırala ve sadece o sayfanın verilerini çek
        var liste = await query.OrderByDescending(x => x.VefatTarihi)
                               .Skip((sayfa - 1) * sayfaBoyutu)
                               .Take(sayfaBoyutu)
                               .ToListAsync();

        // Sayfalama ve arama verilerini HTML (View) tarafına taşı
        ViewBag.MevcutSayfa = sayfa;
        ViewBag.ToplamSayfa = toplamSayfa;
        ViewBag.AramaKelimesi = arama;
        ViewBag.ToplamKayit = toplamKayit;

        return View(liste);
    }

    [HttpPost]
    public async Task<IActionResult> KayitEkle(Kaybettiklerimiz model, IFormFile? Fotograf, DateTime VefatTarihiInput, DateTime? DogumTarihiInput)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

        if (DogumTarihiInput.HasValue && VefatTarihiInput < DogumTarihiInput.Value)
        {
            TempData["Hata"] = "Vefat tarihi, doğum tarihinden önce olamaz.";
            return RedirectToAction(nameof(Index));
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

        model.VefatTarihi = DateOnly.FromDateTime(VefatTarihiInput);
        if (DogumTarihiInput.HasValue)
        {
            model.DogumTarihi = DateOnly.FromDateTime(DogumTarihiInput.Value);
        }

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
    public async Task<IActionResult> KayitGuncelle(Kaybettiklerimiz gelenVeri, IFormFile? Fotograf, DateTime VefatTarihiInput, DateTime? DogumTarihiInput)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null) return RedirectToAction("Login", "Auth");

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