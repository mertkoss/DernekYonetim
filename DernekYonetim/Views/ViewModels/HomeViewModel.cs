using DernekYonetim.Models;
using System.Collections.Generic;

public class HomeViewModel
{
    public DernekHakkindaBolumleri? About { get; set; }
    public List<Uyeler> Uyeler { get; set; }
    public List<Aidatlar> Aidatlar { get; set; }
    public List<DerbisKaydi>? DerbisKayitlari { get; set; }
    public Uyeler? UyeDetay { get; set; }
    public List<Galeri> Galeri { get; internal set; }
}
