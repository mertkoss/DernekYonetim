using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace DernekYonetim.Controllers
{
    public class AidatlarController : Controller
    {
        private readonly DernekYonetimContext _context;

        public AidatlarController(DernekYonetimContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Veritabanından tüm ilişkileriyle beraber üyeleri çekiyoruz
            var uyeler = _context.Uyelers
                .Include(u => u.EgitimMesleks)
                .Include(u => u.DerbisKaydis)
                .Include(u => u.Aidatlars)
                .ToList(); // Veriyi belleğe alıp mapleyeceğiz

            var modelList = new List<UyeListesiViewModel>();

            foreach (var uye in uyeler)
            {
                // Eğitim bilgisini al (Varsa ilkini, yoksa boş)
                var egitim = uye.EgitimMesleks.FirstOrDefault();

                // Derbis bilgisini al
                var derbis = uye.DerbisKaydis.FirstOrDefault();

                // ViewModel oluştur
                var vm = new UyeListesiViewModel
                {
                    UyeId = uye.UyeId,
                    UyeNo = uye.UyeNo,
                    UyelikTarihi = uye.UyelikTarihi,
                    AdSoyad = $"{uye.Ad} {uye.Soyad} {(string.IsNullOrEmpty(uye.EvlilikSoyadi) ? "" : "(" + uye.EvlilikSoyadi + ")")}",
                    TcKimlikNo = uye.TckimlikNo,
                    DogumTarihi = uye.DogumTarihi,
                    DogumYeri = uye.DogumYeri,
                    Telefon = uye.Telefon,
                    Email = uye.Email,
                    AdresTam = $"{uye.Adres} / {uye.Ilce} / {uye.Il}",
                    Vefat = uye.Vefat ?? false,

                    Universite = egitim != null ? $"{egitim.Universite} - {egitim.Fakulte}" : "-",
                    Meslek = egitim?.Meslek ?? "-",
                    MezuniyetYili = egitim?.MezuniyetYili,

                    DerbisDurumu = derbis?.KayitDurumu ?? "Kaydı Yok",

                    // 2025 Aidat Kontrolü
                    Aidat2025 = GetAidatDurumu(uye.Aidatlars, 2025),
                    Aidat2026 = GetAidatDurumu(uye.Aidatlars, 2026),
                    Aidat2027 = GetAidatDurumu(uye.Aidatlars, 2027)
                };

                modelList.Add(vm);
            }

            return View(modelList);
        }

        // Yardımcı Metot: Belirli bir yıl için aidat durumunu çözer
        private AidatDurumu GetAidatDurumu(ICollection<Aidatlar> aidatlar, int yil)
        {
            var aidat = aidatlar.FirstOrDefault(a => a.Yil == yil);
            if (aidat != null && aidat.Durum == "Ödendi")
            {
                return new AidatDurumu { OdendiMi = true, Tutar = aidat.Tutar, DurumAciklamasi = "Ödendi" };
            }
            return new AidatDurumu { OdendiMi = false, Tutar = 0, DurumAciklamasi = "Ödenmedi" };
        }
    }
}