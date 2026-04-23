using DernekYonetim.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DernekYonetim.Filters
{
    public class AuditLogFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. Asıl Controller işleminin çalışmasını ve bitmesini bekle
            var resultContext = await next();

            // 2. Sadece POST işlemleri genelde veritabanını değiştirir (Form Gönderme, Ekleme, Silme)
            var method = context.HttpContext.Request.Method;

            // Eğer işlem başarılıysa (hata fırlatmadıysa) ve bir POST işlemisiyse
            if (method == "POST" && resultContext.Exception == null)
            {
                var controllerName = context.RouteData.Values["controller"]?.ToString();
                var actionName = context.RouteData.Values["action"]?.ToString();

                // 3. Sadece ismi Ekle, Sil, Guncelle veya Mail olan işlemleri yakala
                if (actionName != null && (
                    actionName.Contains("Ekle") ||
                    actionName.Contains("Sil") ||
                    actionName.Contains("Guncelle") ||
                    actionName.Contains("Mail")))
                {
                    try
                    {
                        // DbContext'i filtrenin içine çağırıyoruz
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<DernekYonetimContext>();

                        // Session'dan kullanıcıyı al
                        var adminId = context.HttpContext.Session.GetInt32("AdminID");
                        var adminAd = context.HttpContext.Session.GetString("AdminAd") ?? "Bilinmeyen Yönetici";
                        var ipAdresi = context.HttpContext.Connection.RemoteIpAddress?.ToString();

                        // Aksiyon ismini düzeltelim (Örn: "YeniUyeEkle" -> "Ekleme")
                        string islemTipi = "İşlem";
                        if (actionName.Contains("Ekle")) islemTipi = "Ekleme";
                        else if (actionName.Contains("Sil")) islemTipi = "Silme";
                        else if (actionName.Contains("Guncelle")) islemTipi = "Güncelleme";
                        else if (actionName.Contains("Mail")) islemTipi = "Toplu Mail";

                        // Log Kaydını Oluştur
                        var log = new SistemLoglari
                        {
                            AdminId = adminId,
                            KullaniciAdi = adminAd,
                            IslemTipi = islemTipi,
                            IslemDetayi = $"{controllerName} modülünde [{actionName}] işlemi başarıyla gerçekleştirildi.",
                            IpAdresi = ipAdresi,
                            Tarih = DateTime.Now
                        };

                        dbContext.SistemLoglaris.Add(log);
                        await dbContext.SaveChangesAsync();
                    }
                    catch
                    {
                        // Eğer log yazarken hata olursa, kullanıcının asıl işlemi bozulmasın diye sessizce yutuyoruz
                    }
                }
            }
        }
    }
}