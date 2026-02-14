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

        // Aidat Durumları
        public AidatDurumu Aidat2025 { get; set; }
        public AidatDurumu Aidat2026 { get; set; }
        public AidatDurumu Aidat2027 { get; set; }
    }

    // Yardımcı Class
    public class AidatDurumu
    {
        public bool OdendiMi { get; set; }
        public decimal? Tutar { get; set; }
        public string DurumAciklamasi { get; set; }
    }
}