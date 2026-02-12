using System;
using System.Collections.Generic;

namespace DernekYonetim.Models;

public partial class SosyalMedya
{
    public int Id { get; set; }

    public string? Platform { get; set; }

    public string? Link { get; set; }
}
