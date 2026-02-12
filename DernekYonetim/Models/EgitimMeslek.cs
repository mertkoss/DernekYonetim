using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class EgitimMeslek
{
    public int Id { get; set; }

    public int UyeId { get; set; }

    public string? Universite { get; set; }

    public string? Fakulte { get; set; }

    public int? MezuniyetYili { get; set; }

    public string? Meslek { get; set; }

    public virtual Uyeler Uye { get; set; } = null!;
}
