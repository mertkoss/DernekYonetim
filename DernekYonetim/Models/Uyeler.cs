using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class Uyeler
{
    public int UyeId { get; set; }

    public string UyeNo { get; set; } = null!;

    public DateOnly UyelikTarihi { get; set; }

    public string Ad { get; set; } = null!;

    public string Soyad { get; set; } = null!;

    public string? EvlilikSoyadi { get; set; }

    public string TckimlikNo { get; set; } = null!;

    public DateOnly? DogumTarihi { get; set; }

    public string? DogumYeri { get; set; }

    public string? Telefon { get; set; }

    public string? Email { get; set; }

    public string? Adres { get; set; }

    public string? Ilce { get; set; }

    public string? Il { get; set; }

    public bool? Vefat { get; set; }

    public virtual ICollection<Aidatlar> Aidatlars { get; set; } = new List<Aidatlar>();

    public virtual ICollection<DerbisKaydi> DerbisKaydis { get; set; } = new List<DerbisKaydi>();

    public virtual ICollection<EgitimMeslek> EgitimMesleks { get; set; } = new List<EgitimMeslek>();
}
