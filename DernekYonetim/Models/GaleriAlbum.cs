using System;
using System.Collections.Generic;

namespace DernekYonetim.Models
{
    public class GaleriAlbum
    {
        public int Id { get; set; }

        public string Baslik { get; set; } // Etkinliğin Adı (Klasör Adı)

        public string? Aciklama { get; set; }

        public string? KapakFotografYolu { get; set; } // Klasörün vitrin görseli

        public DateTime OlusturulmaTarihi { get; set; }

        // Bir albümün birden fazla fotoğrafı olur (Bire-Çok İlişki)
        public virtual ICollection<GaleriFotograf> Fotograflar { get; set; }
    }
}