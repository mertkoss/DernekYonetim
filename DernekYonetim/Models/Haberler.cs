using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class Haberler
{
    public int Id { get; set; }

    public string? Baslik { get; set; }

    public string? Ozet { get; set; }

    public string? Icerik { get; set; }

    public string? FotografYolu { get; set; }

    public DateTime? YayimTarihi { get; set; }

    public DateTime? BitisTarihi { get; set; }

    public int? KategoriId { get; set; }

    public virtual HaberKategorileri? Kategori { get; set; }
}
