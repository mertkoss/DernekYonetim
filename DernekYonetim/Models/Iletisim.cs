using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class Iletisim
{
    public int Id { get; set; }

    public string? Adres { get; set; }

    public string? Telefon { get; set; }

    public string? Eposta { get; set; }
}
