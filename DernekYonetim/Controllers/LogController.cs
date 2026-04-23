using DernekYonetim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DernekYonetim.Controllers
{
    // SADECE "SuperAdmin" ROLÜNE SAHİP KULLANICILAR ERİŞEBİLİR
    [Authorize(Roles = "SuperAdmin")]
    public class LogController : Controller
    {
        private readonly DernekYonetimContext _context;

        public LogController(DernekYonetimContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string arama = null)
        {
            // Logları sorgulamak için bir IQueryable oluşturuyoruz
            var sorgu = _context.SistemLoglaris.AsNoTracking().AsQueryable();

            // Eğer yönetici bir arama yaptıysa (örn: belirli bir kullanıcının işlemlerini veya "Silme" işlemlerini arıyorsa)
            if (!string.IsNullOrWhiteSpace(arama))
            {
                sorgu = sorgu.Where(l =>
                    l.KullaniciAdi.Contains(arama) ||
                    l.IslemTipi.Contains(arama) ||
                    l.IslemDetayi.Contains(arama));

                // View'da arama kutusunun dolu kalması için ViewData ile arama terimini gönderiyoruz
                ViewData["ArananKelime"] = arama;
            }

            // En son yapılan işlem en üstte olacak şekilde sırala ve şimdilik son 1000 kaydı getir (Performans için)
            var loglar = await sorgu
                .OrderByDescending(l => l.Tarih)
                .Take(1000)
                .ToListAsync();

            return View(loglar);
        }
    }
}