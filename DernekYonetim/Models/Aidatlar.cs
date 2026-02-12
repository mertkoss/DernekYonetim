using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class Aidatlar
{
    public int Id { get; set; }

    public int UyeId { get; set; }

    public int Yil { get; set; }

    public decimal? Tutar { get; set; }

    public string? Durum { get; set; }

    public virtual Uyeler Uye { get; set; } = null!;
}
