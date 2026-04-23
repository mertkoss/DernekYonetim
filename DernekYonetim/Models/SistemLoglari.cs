using System;

namespace DernekYonetim.Models;

public partial class SistemLoglari
{
    public int LogId { get; set; }

    // İşlemi yapan yöneticinin ID'si (Zorunlu değil, bazen başarısız giriş denemelerini de loglamak isteyebilirsin)
    public int? AdminId { get; set; }

    // İşlemi yapanın adı (Kolay okunabilirlik için)
    public string KullaniciAdi { get; set; } = null!;

    // "Oturum", "Ekleme", "Güncelleme", "Silme", "Toplu Mail" gibi kategoriler
    public string IslemTipi { get; set; } = null!;

    // "Mert yöneticisi 55 numaralı üyeyi sildi." gibi detaylı açıklama
    public string IslemDetayi { get; set; } = null!;

    public DateTime Tarih { get; set; }

    // Güvenlik için işlemi yapan bilgisayarın IP adresi
    public string? IpAdresi { get; set; }
}