using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DernekYonetim.Controllers
{
    public class AidatlarController : Controller
    {
        private readonly DernekYonetimContext _context;

        public AidatlarController(DernekYonetimContext context)
        {
            _context = context;
        }

        // Aidat Listesi
        public IActionResult Index()
        {
            var model = new HomeViewModel
            {
                Aidatlar = _context.Aidatlars
                    .Include(a => a.Uye)
                    .OrderByDescending(a => a.Yil)
                    .ToList()
            };

            return View(model);
        }

        // Üyeye göre aidatlar
        public IActionResult UyeAidatlari(int uyeId)
        {
            var model = new HomeViewModel
            {
                Aidatlar = _context.Aidatlars
                    .Where(a => a.UyeId == uyeId)
                    .Include(a => a.Uye)
                    .OrderByDescending(a => a.Yil)
                    .ToList()
            };

            return View("Index", model);
        }
    }
}
