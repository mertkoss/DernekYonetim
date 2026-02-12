using DernekYonetim.ViewModels;
using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class DernekController : Controller
{
    private readonly DernekYonetimContext _context;

    public DernekController(DernekYonetimContext context)
    {
        _context = context;
    }

    [Route("Dernek/Hakkinda/{slug}")]
    public IActionResult Hakkinda(string slug)
    {
        var bolum = _context.DernekHakkindaBolumleris
            .FirstOrDefault(x => x.Slug == slug && x.Aktif);

        if (bolum == null)
            return NotFound();

        var model = new HomeViewModel
        {
            About = bolum
        };

        return View(model);
    }
}

