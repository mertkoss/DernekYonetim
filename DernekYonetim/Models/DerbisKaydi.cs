using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class DerbisKaydi
{
    public int Id { get; set; }

    public int UyeId { get; set; }

    public string? KayitDurumu { get; set; }

    public virtual Uyeler Uye { get; set; } = null!;
}
