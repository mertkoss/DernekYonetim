using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class DernekHakkindaBolumleri
{
    public int Id { get; set; }

    public string Slug { get; set; } = null!;

    public string Baslik { get; set; } = null!;

    public string? Icerik { get; set; }

    public int Sira { get; set; }

    public bool Aktif { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }
}
