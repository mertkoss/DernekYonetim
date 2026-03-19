using System;
using System.ComponentModel.DataAnnotations;

namespace DernekYonetim.Models
{
    public class IletisimMesaj
    {
        [Key]
        public int Id { get; set; }
        public string AdSoyad { get; set; }
        public string Eposta { get; set; }
        public string Konu { get; set; }
        public string Mesaj { get; set; }
        public DateTime GonderilmeTarihi { get; set; } = DateTime.Now;
        public bool OkunduMu { get; set; } = false;
    }
}