using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DernekYonetim.Controllers
{
    public class DerbisKaydiController : Controller
    {
        private readonly DernekYonetimContext _context;

        public DerbisKaydiController(DernekYonetimContext context)
        {
            _context = context;
        }

        // DERBİS kayıt listesi
        public IActionResult Index()
        {
            var model = new HomeViewModel
            {
                DerbisKayitlari = _context.DerbisKaydis
                    .Include(d => d.Uye)
                    .ToList()
            };

            return View(model);
        }

        // Sadece aktif kayıtlar
        public IActionResult AktifKayitlar()
        {
            var model = new HomeViewModel
            {
                DerbisKayitlari = _context.DerbisKaydis
                    .Include(d => d.Uye)
                    .Where(d => d.KayitDurumu == "Aktif")
                    .ToList()
            };

            return View("Index", model);
        }

    }

}
