using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class AdminKullanicilar
{
    public int AdminId { get; set; }

    public string KullaniciAdi { get; set; } = null!;

    public string SifreHash { get; set; } = null!;

    public string? AdSoyad { get; set; }

    public string? Email { get; set; }

    public bool? AktifMi { get; set; }

    public DateTime? KayitTarihi { get; set; }
}
