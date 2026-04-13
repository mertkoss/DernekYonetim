using DernekYonetim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DernekYonetim.Controllers
{
    [Authorize] // BÜTÜN CONTROLLER'I GÜVENCEYE ALIR: Giriş yapmayan kimse erişemez!
    public class AidatlarController : Controller
    {
        private readonly DernekYonetimContext _context;

        public AidatlarController(DernekYonetimContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME SAYFASI 
        public async Task<IActionResult> Index()
        {
            // Veritabanından veriyi DÜMDÜZ ve TERTEMİZ bir şekilde çekiyoruz. 
            // Select ve Ternary işlemlerini burada yapmıyoruz ki Visual Studio hata ayıklayıcısı çökmesin.
            var uyelerListesi = await _context.Uyelers
                .AsNoTracking()
                .Include(u => u.EgitimMesleks)
                .Include(u => u.DerbisKaydis)
                .Include(u => u.Aidatlars)
                .OrderByDescending(u => u.UyeId)
                .ToListAsync();

            // Veriyi RAM'e aldıktan sonra C# ile güvenle modelliyoruz.
            var modelList = uyelerListesi.Select(uye =>
            {
                var egitim = uye.EgitimMesleks.FirstOrDefault();
                var derbis = uye.DerbisKaydis.FirstOrDefault();

                return new UyeListesiViewModel
                {
                    UyeId = uye.UyeId,
                    UyeNo = uye.UyeNo,
                    UyelikTarihi = uye.UyelikTarihi,
                    AdSoyad = uye.Ad + " " + uye.Soyad,
                    EvlilikSoyadi = uye.EvlilikSoyadi,
                    TcKimlikNo = uye.TckimlikNo,
                    DogumTarihi = uye.DogumTarihi,
                    DogumYeri = uye.DogumYeri,
                    Telefon = uye.Telefon,
                    Email = uye.Email,
                    AdresTam = uye.Adres + " / " + uye.Ilce + " / " + uye.Il,
                    Vefat = uye.Vefat ?? false,
                    // İstifa bilgisini doğrudan View modeline yansıtabilirsin istersen (isteğe bağlı)
                    Universite = egitim != null ? egitim.Universite + " - " + egitim.Fakulte : "-",
                    Meslek = egitim != null ? egitim.Meslek : "-",
                    MezuniyetYili = egitim?.MezuniyetYili,
                    DerbisDurumu = derbis?.KayitDurumu ?? "Kaydı Yok",
                    OdenenAidatlar = uye.Aidatlars.OrderBy(a => a.Yil).Select(a => new AidatOzet { Id = a.Id, Yil = a.Yil, Tutar = a.Tutar }).ToList()
                };
            }).ToList();

            return View(modelList);
        }

        // 2. YENİ ÜYE EKLEME İŞLEMİ (ÇÖKME KORUMALI)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YeniUyeEkle(YeniUyeGirisModel model)
        {
            // GÜVENLİK: Boş TC veya İsim gelirse DB'yi yormadan direkt reddet
            if (string.IsNullOrWhiteSpace(model.Ad) || string.IsNullOrWhiteSpace(model.Soyad) || string.IsNullOrWhiteSpace(model.TckimlikNo) || string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["Hata"] = "Ad, Soyad, TC Kimlik No ve E-Mail alanları zorunludur!";
                return RedirectToAction("Index");
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // 1. Ana Üye Kaydı
                var yeniUye = new Uyeler
                {
                    UyeNo = model.UyeNo,
                    UyelikTarihi = DateOnly.FromDateTime(model.UyelikTarihi),
                    OnayTarihi = model.OnayTarihi.HasValue ? DateOnly.FromDateTime(model.OnayTarihi.Value) : null, // YENİ EKLENDİ
                    Ad = model.Ad,
                    Soyad = model.Soyad,
                    EvlilikSoyadi = model.EvlilikSoyadi,
                    TckimlikNo = model.TckimlikNo,
                    DogumTarihi = model.DogumTarihi.HasValue ? DateOnly.FromDateTime(model.DogumTarihi.Value) : null,
                    DogumYeri = model.DogumYeri,
                    Telefon = model.Telefon,
                    Email = model.Email,
                    Il = model.Il,
                    Ilce = model.Ilce,
                    Adres = model.Adres,
                    Vefat = model.Vefat,
                    IstifaEtti = model.IstifaEtti, // YENİ EKLENDİ
                    IstifaTarihi = model.IstifaTarihi.HasValue ? DateOnly.FromDateTime(model.IstifaTarihi.Value) : null // YENİ EKLENDİ
                };

                _context.Uyelers.Add(yeniUye);
                _context.SaveChanges(); // UyeId'yi almak için önce kaydetmeliyiz.

                // 2. Eğitim Kaydı
                // Lise veya Üniversite alanlarından herhangi biri doluysa Eğitim nesnesi oluşturulur
                if (!string.IsNullOrWhiteSpace(model.Universite) || !string.IsNullOrWhiteSpace(model.Meslek) || !string.IsNullOrWhiteSpace(model.Lise))
                {
                    var egitim = new EgitimMeslek
                    {
                        UyeId = yeniUye.UyeId,
                        Universite = model.Universite,
                        Fakulte = model.Fakulte,
                        Bolum = model.Bolum, // YENİ EKLENDİ
                        MezuniyetYili = model.MezuniyetYili,
                        Lise = model.Lise, // YENİ EKLENDİ
                        LiseMezuniyetYili = model.LiseMezuniyetYili, // YENİ EKLENDİ
                        Meslek = model.Meslek
                    };
                    _context.EgitimMesleks.Add(egitim);
                }

                // 3. Derbis Kaydı
                string? derbisDurum = null;
                if (model.KayitDurumu == "Var") derbisDurum = "Evet";
                else if (model.KayitDurumu == "Yok") derbisDurum = "Hayır";

                var derbis = new DerbisKaydi
                {
                    UyeId = yeniUye.UyeId,
                    KayitDurumu = derbisDurum
                };
                _context.DerbisKaydis.Add(derbis);

                // 4. Aidat Kaydı
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

                // Tüm işlemleri tek seferde veritabanına yaz
                _context.SaveChanges();
                transaction.Commit();

                TempData["Basari"] = "Yeni üye sisteme başarıyla kaydedildi.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // HATA YÖNETİMİ: Çökmeyi engellemek için devasa Exception loglarını sadece güvenli bir şekilde ilk satıra indirgiyoruz.
                transaction.Rollback();
                string hataMesaji = ex.InnerException != null ? ex.InnerException.Message.Split('\n').FirstOrDefault() : ex.Message;

                // Eğer veritabanı kısıtlamasına takıldıysa (Örn: Aynı TC Kimlik numarası)
                if (hataMesaji != null && (hataMesaji.Contains("UNIQUE") || hataMesaji.Contains("Duplicate")))
                {
                    TempData["Hata"] = "Bu TC Kimlik Numarası veya Üye No sistemde zaten kayıtlı!";
                }
                else
                {
                    TempData["Hata"] = "Kayıt sırasında bir veritabanı hatası oluştu: " + hataMesaji;
                }

                return RedirectToAction("Index");
            }
        }

        // 3. AJAX İÇİN JSON VERİ GETİRME
        [HttpGet]
        public IActionResult UyeGetir(int id)
        {
            var uye = _context.Uyelers
                .Include(u => u.EgitimMesleks)
                .Include(u => u.DerbisKaydis)
                .Include(u => u.Aidatlars)
                .FirstOrDefault(x => x.UyeId == id);

            if (uye == null) return NotFound();

            var egitim = uye.EgitimMesleks.FirstOrDefault();
            var derbis = uye.DerbisKaydis.FirstOrDefault();

            string uiKayitDurumu = "Belirtilmedi";
            if (derbis?.KayitDurumu == "Evet") uiKayitDurumu = "Var";
            else if (derbis?.KayitDurumu == "Hayır") uiKayitDurumu = "Yok";

            var data = new
            {
                uyeId = uye.UyeId,
                uyeNo = uye.UyeNo,
                uyelikTarihi = uye.UyelikTarihi.ToString("yyyy-MM-dd"),
                onayTarihi = uye.OnayTarihi.HasValue ? uye.OnayTarihi.Value.ToString("yyyy-MM-dd") : "", // YENİ
                istifaEtti = uye.IstifaEtti, // YENİ
                istifaTarihi = uye.IstifaTarihi.HasValue ? uye.IstifaTarihi.Value.ToString("yyyy-MM-dd") : "", // YENİ
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
                bolum = egitim?.Bolum, // YENİ
                lise = egitim?.Lise, // YENİ
                liseMezuniyetYili = egitim?.LiseMezuniyetYili, // YENİ
                mezuniyetYili = egitim?.MezuniyetYili,
                meslek = egitim?.Meslek,
                kayitDurumu = uiKayitDurumu,
                gecmisAidatlar = uye.Aidatlars.OrderBy(x => x.Yil).Select(x => new {
                    id = x.Id,
                    yil = x.Yil,
                    tutar = x.Tutar
                }).ToList()
            };

            return Json(data);
        }

        // 4. ÜYE GÜNCELLEME İŞLEMİ (ÇÖKME KORUMALI)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UyeGuncelle(YeniUyeGirisModel model, int UyeId)
        {
            if (UyeId <= 0 || string.IsNullOrWhiteSpace(model.Ad) || string.IsNullOrWhiteSpace(model.TckimlikNo))
            {
                TempData["Hata"] = "Geçersiz üye bilgisi veya eksik zorunlu alanlar.";
                return RedirectToAction("Index");
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var uye = _context.Uyelers.Find(UyeId);
                if (uye == null)
                {
                    TempData["Hata"] = "Güncellenecek üye bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Temel Bilgiler ve Yeni Alanlar
                uye.UyeNo = model.UyeNo;
                uye.UyelikTarihi = DateOnly.FromDateTime(model.UyelikTarihi);
                uye.OnayTarihi = model.OnayTarihi.HasValue ? DateOnly.FromDateTime(model.OnayTarihi.Value) : null;
                uye.TckimlikNo = model.TckimlikNo;
                uye.Ad = model.Ad;
                uye.Soyad = model.Soyad;
                uye.EvlilikSoyadi = model.EvlilikSoyadi;
                uye.DogumTarihi = model.DogumTarihi.HasValue ? DateOnly.FromDateTime(model.DogumTarihi.Value) : null;
                uye.DogumYeri = model.DogumYeri;
                uye.Vefat = model.Vefat;
                uye.IstifaEtti = model.IstifaEtti;
                uye.IstifaTarihi = model.IstifaTarihi.HasValue ? DateOnly.FromDateTime(model.IstifaTarihi.Value) : null;
                uye.Telefon = model.Telefon;
                uye.Email = model.Email;
                uye.Il = model.Il;
                uye.Ilce = model.Ilce;
                uye.Adres = model.Adres;

                _context.Uyelers.Update(uye);

                // Eğitim Bilgileri
                var egitim = _context.EgitimMesleks.FirstOrDefault(x => x.UyeId == UyeId);
                if (egitim != null)
                {
                    egitim.Universite = model.Universite;
                    egitim.Fakulte = model.Fakulte;
                    egitim.Bolum = model.Bolum;
                    egitim.MezuniyetYili = model.MezuniyetYili;
                    egitim.Lise = model.Lise;
                    egitim.LiseMezuniyetYili = model.LiseMezuniyetYili;
                    egitim.Meslek = model.Meslek;
                    _context.EgitimMesleks.Update(egitim);
                }
                else if (!string.IsNullOrWhiteSpace(model.Universite) || !string.IsNullOrWhiteSpace(model.Meslek) || !string.IsNullOrWhiteSpace(model.Lise))
                {
                    var yeniEgitim = new EgitimMeslek
                    {
                        UyeId = UyeId,
                        Universite = model.Universite,
                        Fakulte = model.Fakulte,
                        Bolum = model.Bolum,
                        MezuniyetYili = model.MezuniyetYili,
                        Lise = model.Lise,
                        LiseMezuniyetYili = model.LiseMezuniyetYili,
                        Meslek = model.Meslek
                    };
                    _context.EgitimMesleks.Add(yeniEgitim);
                }

                // Derbis Kaydı
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

                // Yeni Aidat Ekleme (Güncelleme ekranından)
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

                TempData["Basari"] = "Üye bilgileri başarıyla güncellendi.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                string hataMesaji = ex.InnerException != null ? ex.InnerException.Message.Split('\n').FirstOrDefault() : ex.Message;

                if (hataMesaji != null && hataMesaji.Contains("UNIQUE"))
                {
                    TempData["Hata"] = "Bu TC Kimlik Numarası veya Üye No sistemde zaten kullanılıyor!";
                }
                else
                {
                    TempData["Hata"] = "Güncelleme sırasında bir hata oluştu: " + hataMesaji;
                }
                return RedirectToAction("Index");
            }
        }

        // 5. ÜYE SİLME İŞLEMİ (GÜVENLİ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UyeSil(int id)
        {
            var uye = _context.Uyelers
                .Include(u => u.EgitimMesleks)
                .Include(u => u.DerbisKaydis)
                .Include(u => u.Aidatlars)
                .FirstOrDefault(x => x.UyeId == id);

            if (uye == null) return NotFound();

            if (uye.EgitimMesleks.Any()) _context.EgitimMesleks.RemoveRange(uye.EgitimMesleks);
            if (uye.DerbisKaydis.Any()) _context.DerbisKaydis.RemoveRange(uye.DerbisKaydis);
            if (uye.Aidatlars.Any()) _context.Aidatlars.RemoveRange(uye.Aidatlars);

            _context.Uyelers.Remove(uye);
            _context.SaveChanges();

            TempData["Basari"] = "Üye ve bağlı tüm kayıtlar başarıyla silindi.";
            return RedirectToAction("Index");
        }

        // 6. TEKİL AİDAT SİLME (GÜVENLİ AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AidatSil(int id)
        {
            var aidat = _context.Aidatlars.Find(id);
            if (aidat != null)
            {
                _context.Aidatlars.Remove(aidat);
                _context.SaveChanges();
                return Ok("Silindi");
            }
            return NotFound();
        }
    }
}