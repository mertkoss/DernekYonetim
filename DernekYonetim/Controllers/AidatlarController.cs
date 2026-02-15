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

        // 2. YENİ ÜYE EKLEME İŞLEMİ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YeniUyeEkle(YeniUyeGirisModel model)
        {
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
                var yeniUye = new Uyeler
                {
                    UyeNo = model.UyeNo,
                    UyelikTarihi = DateOnly.FromDateTime(model.UyelikTarihi),
                    Ad = model.Ad,
                    Soyad = model.Soyad,
                    EvlilikSoyadi = model.EvlilikSoyadi,
                    TckimlikNo = model.TckimlikNo,
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
                // Arayüz: "Var/Yok/Belirtilmedi" -> DB: "Evet/Hayır/NULL"
                string? derbisDurum = null; // Varsayılan (Belirtilmedi)

                if (model.KayitDurumu == "Var")
                {
                    derbisDurum = "Evet";
                }
                else if (model.KayitDurumu == "Yok")
                {
                    derbisDurum = "Hayır";
                }

                var derbis = new DerbisKaydi
                {
                    UyeId = yeniUye.UyeId,
                    KayitDurumu = derbisDurum
                };
                _context.DerbisKaydis.Add(derbis);

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
                transaction.Rollback();
                string detayliHata = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest("Kayıt Hatası Detayı: " + detayliHata);
            }
        } // <--- BURASI! Önceki metot burada bitmeli.

        [HttpGet]
        public IActionResult UyeGetir(int id)
        {
            var uye = _context.Uyelers
                .Include(u => u.EgitimMesleks)
                .Include(u => u.DerbisKaydis)
                .Include(u => u.Aidatlars) // Aidatları çekmeyi unutma!
                .FirstOrDefault(x => x.UyeId == id);

            if (uye == null) return NotFound();

            var egitim = uye.EgitimMesleks.FirstOrDefault();
            var derbis = uye.DerbisKaydis.FirstOrDefault();

            // DB: "Evet/Hayır" -> UI: "Var/Yok" Çevirisi
            string uiKayitDurumu = "Belirtilmedi";
            if (derbis?.KayitDurumu == "Evet") uiKayitDurumu = "Var";
            else if (derbis?.KayitDurumu == "Hayır") uiKayitDurumu = "Yok";

            var data = new
            {
                uyeId = uye.UyeId,
                uyeNo = uye.UyeNo,
                uyelikTarihi = uye.UyelikTarihi.ToString("yyyy-MM-dd"),
                tcKimlikNo = uye.TckimlikNo,
                ad = uye.Ad,
                soyad = uye.Soyad,
                evlilikSoyadi = uye.EvlilikSoyadi,
                dogumTarihi = uye.DogumTarihi.HasValue ? uye.DogumTarihi.Value.ToString("yyyy-MM-dd") : "",
                dogumYeri = uye.DogumYeri,
                vefat = uye.Vefat ?? false,
                telefon = uye.Telefon,
                email = uye.Email,
                il = uye.Il,
                ilce = uye.Ilce,
                adres = uye.Adres,
                universite = egitim?.Universite,
                fakulte = egitim?.Fakulte,
                mezuniyetYili = egitim?.MezuniyetYili,
                meslek = egitim?.Meslek,
                kayitDurumu = uiKayitDurumu,

                // YENİ EKLENEN KISIM: Geçmiş Aidatları Listele
                gecmisAidatlar = uye.Aidatlars.OrderBy(x => x.Yil).Select(x => new {
                    yil = x.Yil,
                    tutar = x.Tutar
                }).ToList()
            };

            return Json(data);
        }

        // 4. ÜYE GÜNCELLEME İŞLEMİ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UyeGuncelle(YeniUyeGirisModel model, int UyeId)
        {
            // using kullandığımız için işlem sonunda veya hatada otomatik temizlik yapılır.
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var uye = _context.Uyelers.Find(UyeId);
                if (uye == null) return NotFound("Üye bulunamadı.");

                // A) Temel Bilgileri Güncelle
                uye.UyeNo = model.UyeNo;
                uye.UyelikTarihi = DateOnly.FromDateTime(model.UyelikTarihi);
                uye.TckimlikNo = model.TckimlikNo;
                uye.Ad = model.Ad;
                uye.Soyad = model.Soyad;
                uye.EvlilikSoyadi = model.EvlilikSoyadi;
                uye.DogumTarihi = model.DogumTarihi.HasValue ? DateOnly.FromDateTime(model.DogumTarihi.Value) : null;
                uye.DogumYeri = model.DogumYeri;
                uye.Vefat = model.Vefat;
                uye.Telefon = model.Telefon;
                uye.Email = model.Email;
                uye.Il = model.Il;
                uye.Ilce = model.Ilce;
                uye.Adres = model.Adres;

                _context.Uyelers.Update(uye);

                // B) Eğitim Bilgilerini Güncelle
                var egitim = _context.EgitimMesleks.FirstOrDefault(x => x.UyeId == UyeId);
                if (egitim != null)
                {
                    egitim.Universite = model.Universite;
                    egitim.Fakulte = model.Fakulte;
                    egitim.MezuniyetYili = model.MezuniyetYili;
                    egitim.Meslek = model.Meslek;
                    _context.EgitimMesleks.Update(egitim);
                }
                else
                {
                    if (!string.IsNullOrEmpty(model.Universite) || !string.IsNullOrEmpty(model.Meslek))
                    {
                        var yeniEgitim = new EgitimMeslek
                        {
                            UyeId = UyeId,
                            Universite = model.Universite,
                            Fakulte = model.Fakulte,
                            MezuniyetYili = model.MezuniyetYili,
                            Meslek = model.Meslek
                        };
                        _context.EgitimMesleks.Add(yeniEgitim);
                    }
                }

                // C) Derbis Güncelle (Var/Yok -> Evet/Hayır Çevirisi)
                var derbis = _context.DerbisKaydis.FirstOrDefault(x => x.UyeId == UyeId);
                string? derbisDurum = null;

                if (model.KayitDurumu == "Var") derbisDurum = "Evet";
                else if (model.KayitDurumu == "Yok") derbisDurum = "Hayır";

                if (derbis != null)
                {
                    derbis.KayitDurumu = derbisDurum;
                    _context.DerbisKaydis.Update(derbis);
                }
                else
                {
                    var yeniDerbis = new DerbisKaydi { UyeId = UyeId, KayitDurumu = derbisDurum };
                    _context.DerbisKaydis.Add(yeniDerbis);
                }

                // D) AİDAT GÜNCELLEME / EKLEME
                // Düzenleme ekranında alt kısma girilen Yıl ve Tutar, yeni bir aidat satırı olarak eklenir.
                if (model.AidatTutari > 0)
                {
                    var yeniAidat = new Aidatlar
                    {
                        UyeId = UyeId,
                        Yil = model.AidatYili ?? DateTime.Now.Year,
                        Tutar = model.AidatTutari,
                        Durum = "Ödendi"
                    };
                    _context.Aidatlars.Add(yeniAidat);
                }

                _context.SaveChanges();
                transaction.Commit();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // DÜZELTME: transaction.Rollback(); BURADAN SİLİNDİ.
                // using bloğu hata durumunda işlemi otomatik geri alır.

                string detayliHata = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest("Güncelleme Hatası Detayı: " + detayliHata);
            }
        
    }
    }
}