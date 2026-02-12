using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DernekYonetim.Models;

public class IletisimController : Controller
{
    private readonly DernekYonetimContext _context;

    public IletisimController(DernekYonetimContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Genelde tek bir iletişim kaydı olur (First)
        var iletisim = await _context.Iletisims.FirstOrDefaultAsync();
        return View(iletisim);
    }

    // Bilgileri Güncelleme (Yönetim için)
    [HttpPost]
    public async Task<IActionResult> Guncelle(Iletisim model)
    {
        if (HttpContext.Session.GetInt32("AdminID") == null)
        {
            return RedirectToAction("Login", "Auth");
        }
        var mevcut = await _context.Iletisims.FirstOrDefaultAsync();
        if (mevcut != null)
        {
            mevcut.Adres = model.Adres;
            mevcut.Telefon = model.Telefon;
            mevcut.Eposta = model.Eposta;
            _context.Iletisims.Update(mevcut);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}