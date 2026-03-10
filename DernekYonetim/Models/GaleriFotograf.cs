using System;

namespace DernekYonetim.Models
{
    public class GaleriFotograf
    {
        public int Id { get; set; }

        public int AlbumId { get; set; } // Bu fotoğraf hangi klasöre ait?

        public string? Aciklama { get; set; }

        public string FotografYolu { get; set; }

        public DateTime YuklemeTarihi { get; set; }

        // Fotoğrafın bağlı olduğu Ana Albüm (Klasör) nesnesi
        public virtual GaleriAlbum Album { get; set; }
    }
}