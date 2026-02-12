using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class HaberKategorileri
{
    public int Id { get; set; }

    public string? KategoriAdi { get; set; }

    public virtual ICollection<Haberler> Haberlers { get; set; } = new List<Haberler>();
}
