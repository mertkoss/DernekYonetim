using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class Kaybettiklerimiz
{
    public int Id { get; set; }

    public string? AdSoyad { get; set; }

    public DateOnly? VefatTarihi { get; set; }

    public string? Aciklama { get; set; }

    public string? FotografYolu { get; set; }
}
