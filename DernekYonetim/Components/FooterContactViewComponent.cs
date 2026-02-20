using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DernekYonetim.Components
{
    public class FooterContactViewComponent : ViewComponent
    {
        private readonly DernekYonetimContext _context;

        public FooterContactViewComponent(DernekYonetimContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Veritabanından iletişim bilgilerini çekiyoruz
            var iletisim = await _context.Iletisims.FirstOrDefaultAsync();
            return View(iletisim);
        }
    }
}