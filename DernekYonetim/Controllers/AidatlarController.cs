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
            var uyeler = _context.Uyelers
                .Include(u => u.EgitimMesleks)
                .Include(u => u.DerbisKaydis)
                .Include(u => u.Aidatlars)
                .ToList();

            var modelList = new List<UyeListesiViewModel>();

            foreach (var uye in uyeler)
            {
                var egitim = uye.EgitimMesleks.FirstOrDefault();
                var derbis = uye.DerbisKaydis.FirstOrDefault();

                var vm = new UyeListesiViewModel
                {
                    UyeId = uye.UyeId,
                    UyeNo = uye.UyeNo,
                    UyelikTarihi = uye.UyelikTarihi,
                    AdSoyad = $"{uye.Ad} {uye.Soyad}",
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

                    // BURASI DEĞİŞTİ: Aidatları db'den çekip listeye atıyoruz
                    OdenenAidatlar = uye.Aidatlars
                                        .OrderBy(x => x.Yil)
                                        .Select(a => new AidatOzet { Yil = a.Yil, Tutar = a.Tutar })
                                        .ToList()
                };

                modelList.Add(vm);
            }

            return View(modelList);
        }

        // YENİ EKLENEN METOT: Formdan gelen veriyi kaydeder
        [HttpPost]
        public IActionResult AidatEkle(int uyeId, int yil, decimal tutar)
        {
            // Önce bu üyeye ait o yılın kaydı var mı bakalım
            var mevcutAidat = _context.Aidatlars.FirstOrDefault(x => x.UyeId == uyeId && x.Yil == yil);

            if (mevcutAidat != null)
            {
                // Varsa güncelle
                mevcutAidat.Tutar = tutar;
                mevcutAidat.Durum = "Ödendi";
            }
            else
            {
                // Yoksa yeni ekle
                var yeniAidat = new Aidatlar
                {
                    UyeId = uyeId,
                    Yil = yil,
                    Tutar = tutar,
                    Durum = "Ödendi"
                };
                _context.Aidatlars.Add(yeniAidat);
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}