using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class Galeri
{
    public int Id { get; set; }

    public string? Baslik { get; set; }

    public string? Aciklama { get; set; }

    public string? FotografYolu { get; set; }

    public DateTime? YuklemeTarihi { get; set; }
}
