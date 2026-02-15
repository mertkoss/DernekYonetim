using System;
using System.Collections.Generic;

namespace DernekYonetim.Models
{
    // Ana View Modelimiz
    public class HomeViewModel
    {
        public DernekHakkindaBolumleri? About { get; set; }
        public List<Uyeler> Uyeler { get; set; }
        public List<Aidatlar> Aidatlar { get; set; }
        public List<DerbisKaydi>? DerbisKayitlari { get; set; }
        public Uyeler? UyeDetay { get; set; }
        public List<Galeri> Galeri { get; internal set; }

        // YENİ EKLEME:
        // Excel tablosu gibi olan özel listeyi burada tutabiliriz.
        // Böylece sayfaya hem bu listeyi hem de diğer verileri aynı anda gönderebilirsin.
        public List<UyeListesiViewModel> OzelUyeListesi { get; set; }
    }

    // Tablo için özel oluşturduğumuz model
    public class UyeListesiViewModel
    {
        // Temel Bilgiler
        public int UyeId { get; set; }
        public string UyeNo { get; set; }
        public DateOnly UyelikTarihi { get; set; }
        public string AdSoyad { get; set; }
        public string TcKimlikNo { get; set; }

        // Kişisel & İletişim (Detayda görünecekler)
        public DateOnly? DogumTarihi { get; set; }
        public string DogumYeri { get; set; }
        public string Telefon { get; set; }
        public string Email { get; set; }
        public string AdresTam { get; set; }
        public bool Vefat { get; set; }

        // Eğitim & İş
        public string Universite { get; set; }
        public string Meslek { get; set; }
        public int? MezuniyetYili { get; set; }

        // Derbis
        public string DerbisDurumu { get; set; }

        public List<AidatOzet> OdenenAidatlar { get; set; } = new List<AidatOzet>();
    }

    public class AidatOzet
    {
        public int Yil { get; set; }
        public decimal? Tutar { get; set; }
    }

    // Yardımcı Class
    public class AidatDurumu
    {
        public bool OdendiMi { get; set; }
        public decimal? Tutar { get; set; }
        public string DurumAciklamasi { get; set; }
    }
    public class YeniUyeGirisModel
    {
        // Kimlik
        public string UyeNo { get; set; }

        // BURASI DEĞİŞTİ: DateOnly yerine DateTime
        public DateTime UyelikTarihi { get; set; }
        public string TckimlikNo { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string? EvlilikSoyadi { get; set; }

        // BURASI DEĞİŞTİ: DateOnly? yerine DateTime?
        public DateTime? DogumTarihi { get; set; }
        public string? DogumYeri { get; set; }
        public bool Vefat { get; set; }

        // İletişim
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Il { get; set; }
        public string? Ilce { get; set; }
        public string? Adres { get; set; }

        // Eğitim
        public string? Universite { get; set; }
        public string? Fakulte { get; set; }
        public int? MezuniyetYili { get; set; }
        public string? Meslek { get; set; }

        // Diğer
        public string? KayitDurumu { get; set; }
        public int? AidatYili { get; set; }
        public decimal? AidatTutari { get; set; }
    }

}