using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DernekYonetim.Controllers
{
    public class UyelerController : Controller
    {
        private readonly DernekYonetimContext _context;

        public UyelerController(DernekYonetimContext context)
        {
            _context = context;
        }

        // Üye Listesi
        public IActionResult Index()
        {
            var model = new HomeViewModel
            {
                Uyeler = _context.Uyelers.ToList()
            };

            return View(model);
        }

        // Üye Detay
        public IActionResult Detay(int id)
        {
            var uye = _context.Uyelers
                .Include(x => x.Aidatlars)
                .Include(x => x.DerbisKaydis)
                .FirstOrDefault(x => x.UyeId == id);

            if (uye == null)
                return NotFound();

            var model = new HomeViewModel
            {
                UyeDetay = uye
            };

            return View(model);
        }

        // Test Admin
        public IActionResult TestAdmin()
        {
            using SqlConnection con = new SqlConnection(
                "Server=.;Database=DernekYonetimDB;Trusted_Connection=True;");

            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM AdminKullanicilar", con);
            int sayi = (int)cmd.ExecuteScalar();

            ViewBag.AdminSayisi = sayi;
            return View();
        }
    }
}
