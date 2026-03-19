using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DernekYonetim.Controllers
{
    public class IletisimController : Controller
    {
        private readonly DernekYonetimContext _context;

        public IletisimController(DernekYonetimContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var iletisim = await _context.Iletisims.FirstOrDefaultAsync();
            return View(iletisim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

                TempData["Basari"] = "İletişim bilgileri başarıyla güncellendi.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MesajGonder(string AdSoyad, string Eposta, string Konu, string Mesaj)
        {
            try
            {
                // Mail atmak yerine veritabanına kaydediyoruz
                var yeniMesaj = new IletisimMesaj
                {
                    AdSoyad = System.Net.WebUtility.HtmlEncode(AdSoyad),
                    Eposta = System.Net.WebUtility.HtmlEncode(Eposta),
                    Konu = System.Net.WebUtility.HtmlEncode(Konu),
                    Mesaj = System.Net.WebUtility.HtmlEncode(Mesaj),
                    GonderilmeTarihi = DateTime.Now,
                    OkunduMu = false
                };

                _context.IletisimMesajlari.Add(yeniMesaj);
                await _context.SaveChangesAsync();

                TempData["Basari"] = "Mesajınız başarıyla iletildi. En kısa sürede tarafınıza dönüş yapılacaktır.";
            }
            catch (Exception ex)
            {
                string hataDetayi = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Hata"] = "Mesaj kaydedilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin. (" + hataDetayi + ")";
            }

            return RedirectToAction("Index");
        }

        // --- YENİ EKLENEN ADMİN METOTLARI ---

        // Sadece Adminler Görebilir
        public async Task<IActionResult> GelenMesajlar()
        {
            if (HttpContext.Session.GetInt32("AdminID") == null)
                return RedirectToAction("Login", "Auth");

            var mesajlar = await _context.IletisimMesajlari
                .OrderByDescending(m => m.GonderilmeTarihi)
                .ToListAsync();

            return View(mesajlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MesajSil(int id)
        {
            if (HttpContext.Session.GetInt32("AdminID") == null)
                return RedirectToAction("Login", "Auth");

            var mesaj = await _context.IletisimMesajlari.FindAsync(id);
            if (mesaj != null)
            {
                _context.IletisimMesajlari.Remove(mesaj);
                await _context.SaveChangesAsync();
                TempData["Basari"] = "Mesaj başarıyla silindi.";
            }

            return RedirectToAction(nameof(GelenMesajlar));
        }

        [HttpPost]
        public async Task<IActionResult> MesajOkunduIsaretle(int id)
        {
            if (HttpContext.Session.GetInt32("AdminID") == null) return Json(new { success = false });

            var mesaj = await _context.IletisimMesajlari.FindAsync(id);
            if (mesaj != null && !mesaj.OkunduMu)
            {
                mesaj.OkunduMu = true;
                _context.Update(mesaj);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }
    }
}