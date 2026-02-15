using DernekYonetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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

        // 1. LİSTELEME SAYFASI
        public IActionResult Index()
        {
            // Veritabanından tüm ilişkili verileri çekiyoruz
            var uyeler = _context.Uyelers
                .Include(u => u.EgitimMesleks)
                .Include(u => u.DerbisKaydis)
                .Include(u => u.Aidatlars)
                .OrderByDescending(u => u.UyeId) // En son eklenen en üstte görünsün
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
                    // Evlilik soyadı varsa parantez içinde ekleyelim, yoksa boş geçelim
                    // İstersen: AdSoyad = $"{uye.Ad} {uye.Soyad} {(string.IsNullOrEmpty(uye.EvlilikSoyadi) ? "" : "(" + uye.EvlilikSoyadi + ")")}",
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

                    OdenenAidatlar = uye.Aidatlars
                                        .OrderBy(x => x.Yil)
                                        .Select(a => new AidatOzet { Yil = a.Yil, Tutar = a.Tutar })
                                        .ToList()
                };

                modelList.Add(vm);
            }

            return View(modelList);
        }

        // 2. YENİ ÜYE EKLEME İŞLEMİ (MODAL FORMUNDAN GELEN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YeniUyeEkle(YeniUyeGirisModel model)
        {
            // Hata varsa görmek için breakpoint'i buraya koy
            if (!ModelState.IsValid)
            {
                var hatalar = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest("Form Hatası: " + hatalar);
            }

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // A) ÜYE TABLOSUNA KAYIT
                var yeniUye = new Uyeler
                {
                    UyeNo = model.UyeNo,
                    // ÇEVİRME İŞLEMİ BURADA:
                    UyelikTarihi = DateOnly.FromDateTime(model.UyelikTarihi),

                    Ad = model.Ad,
                    Soyad = model.Soyad,
                    EvlilikSoyadi = model.EvlilikSoyadi,
                    TckimlikNo = model.TckimlikNo,

                    // NULL KONTROLLÜ ÇEVİRME:
                    DogumTarihi = model.DogumTarihi.HasValue
                                  ? DateOnly.FromDateTime(model.DogumTarihi.Value)
                                  : null,

                    DogumYeri = model.DogumYeri,
                    Telefon = model.Telefon,
                    Email = model.Email,
                    Il = model.Il,
                    Ilce = model.Ilce,
                    Adres = model.Adres,
                    Vefat = model.Vefat
                };

                _context.Uyelers.Add(yeniUye);
                _context.SaveChanges();

                // B) EĞİTİM
                if (!string.IsNullOrEmpty(model.Universite) || !string.IsNullOrEmpty(model.Meslek))
                {
                    var egitim = new EgitimMeslek
                    {
                        UyeId = yeniUye.UyeId,
                        Universite = model.Universite,
                        Fakulte = model.Fakulte,
                        MezuniyetYili = model.MezuniyetYili,
                        Meslek = model.Meslek
                    };
                    _context.EgitimMesleks.Add(egitim);
                }

                // C) DERBİS TABLOSUNA KAYIT
                // ÇÖZÜM BURADA: Eğer "Kaydı Yok" seçildiyse veritabanına null gönderiyoruz.
                string? derbisDurum = null;
                if (model.KayitDurumu == "Evet" || model.KayitDurumu == "Hayır")
                {
                    derbisDurum = model.KayitDurumu;
                }

                var derbis = new DerbisKaydi
                {
                    UyeId = yeniUye.UyeId,
                    KayitDurumu = derbisDurum
                };
                _context.DerbisKaydis.Add(derbis);

                // D) AİDAT
                if (model.AidatTutari > 0)
                {
                    var aidat = new Aidatlar
                    {
                        UyeId = yeniUye.UyeId,
                        Yil = model.AidatYili ?? DateTime.Now.Year,
                        Tutar = model.AidatTutari,
                        Durum = "Ödendi"
                    };
                    _context.Aidatlars.Add(aidat);
                }

                _context.SaveChanges();
                transaction.Commit();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // İşlemi geri al
                transaction.Rollback();

                // Asıl hatayı (InnerException) bulup ekrana basalım
                string detayliHata = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                return BadRequest("Kayıt Hatası Detayı: " + detayliHata);
            }

        }
    }
}
