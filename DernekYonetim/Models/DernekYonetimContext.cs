using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DernekYonetim.Models;

public partial class DernekYonetimContext : DbContext
{
    public DernekYonetimContext()
    {
    }

    public DernekYonetimContext(DbContextOptions<DernekYonetimContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminKullanicilar> AdminKullanicilars { get; set; }

    public virtual DbSet<Aidatlar> Aidatlars { get; set; }

    public virtual DbSet<DerbisKaydi> DerbisKaydis { get; set; }

    public virtual DbSet<DernekHakkindaBolumleri> DernekHakkindaBolumleris { get; set; }

    public virtual DbSet<EgitimMeslek> EgitimMesleks { get; set; }

    public virtual DbSet<Galeri> Galeris { get; set; }

    public virtual DbSet<HaberKategorileri> HaberKategorileris { get; set; }

    public virtual DbSet<Haberler> Haberlers { get; set; }

    public virtual DbSet<Iletisim> Iletisims { get; set; }

    public virtual DbSet<Kaybettiklerimiz> Kaybettiklerimizs { get; set; }

    public virtual DbSet<SosyalMedya> SosyalMedyas { get; set; }

    public virtual DbSet<Uyeler> Uyelers { get; set; }

    public virtual DbSet<ZiyaretciSayaci> ZiyaretciSayacis { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=DernekYonetimDB;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminKullanicilar>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__AdminKul__719FE4E808FB369B");

            entity.ToTable("AdminKullanicilar");

            entity.HasIndex(e => e.KullaniciAdi, "UQ__AdminKul__5BAE6A75D4833C62").IsUnique();

            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.AdSoyad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.AktifMi).HasDefaultValue(true);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.KayitTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.KullaniciAdi)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SifreHash)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Aidatlar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Aidatlar__3214EC276BCABFB7");

            entity.ToTable("Aidatlar");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Durum)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Tutar).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UyeId).HasColumnName("UyeID");

            entity.HasOne(d => d.Uye).WithMany(p => p.Aidatlars)
                .HasForeignKey(d => d.UyeId)
                .HasConstraintName("FK_Aidatlar_Uyeler");
        });

        modelBuilder.Entity<DerbisKaydi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DerbisKa__3214EC27CA89B90C");

            entity.ToTable("DerbisKaydi");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.KayitDurumu)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.UyeId).HasColumnName("UyeID");

            entity.HasOne(d => d.Uye).WithMany(p => p.DerbisKaydis)
                .HasForeignKey(d => d.UyeId)
                .HasConstraintName("FK_DerbisKaydi_Uyeler");
        });

        modelBuilder.Entity<DernekHakkindaBolumleri>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DernekHa__3214EC07B60F4D3C");

            entity.ToTable("DernekHakkindaBolumleri");

            entity.HasIndex(e => e.Slug, "UQ__DernekHa__BC7B5FB6DD26EB1F").IsUnique();

            entity.Property(e => e.Aktif).HasDefaultValue(true);
            entity.Property(e => e.Baslik).HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(100);
        });

        modelBuilder.Entity<EgitimMeslek>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EgitimMe__3214EC27A5E00571");

            entity.ToTable("EgitimMeslek");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Fakulte)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Meslek)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Universite)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UyeId).HasColumnName("UyeID");

            entity.HasOne(d => d.Uye).WithMany(p => p.EgitimMesleks)
                .HasForeignKey(d => d.UyeId)
                .HasConstraintName("FK_EgitimMeslek_Uyeler");
        });

        modelBuilder.Entity<Galeri>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Galeri__3214EC272F64344B");

            entity.ToTable("Galeri");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Baslik)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FotografYolu)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.YuklemeTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<HaberKategorileri>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HaberKat__3214EC27DBE51158");

            entity.ToTable("HaberKategorileri");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.KategoriAdi)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Haberler>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Haberler__3214EC277BF3D2B8");

            entity.ToTable("Haberler");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Baslik)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.BitisTarihi).HasColumnType("datetime");
            entity.Property(e => e.FotografYolu)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.KategoriId).HasColumnName("KategoriID");
            entity.Property(e => e.Ozet).HasMaxLength(255);
            entity.Property(e => e.YayimTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Kategori).WithMany(p => p.Haberlers)
                .HasForeignKey(d => d.KategoriId)
                .HasConstraintName("FK_Haberler_Kategoriler");
        });

        modelBuilder.Entity<Iletisim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Iletisim__3214EC27BE87DAA3");

            entity.ToTable("Iletisim");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Adres).HasMaxLength(255);
            entity.Property(e => e.Eposta)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Telefon)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Kaybettiklerimiz>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Kaybetti__3214EC27EE0F9452");

            entity.ToTable("Kaybettiklerimiz");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AdSoyad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FotografYolu)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SosyalMedya>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SosyalMe__3214EC274888AE5A");

            entity.ToTable("SosyalMedya");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Link)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Uyeler>(entity =>
        {
            entity.HasKey(e => e.UyeId).HasName("PK__Uyeler__76F7D9EF3F58ED03");

            entity.ToTable("Uyeler");

            entity.HasIndex(e => e.UyeNo, "UQ__Uyeler__76F600CE67A956AD").IsUnique();

            entity.HasIndex(e => e.TckimlikNo, "UQ__Uyeler__7E1935EDF83F30E2").IsUnique();

            entity.Property(e => e.UyeId).HasColumnName("UyeID");
            entity.Property(e => e.Ad)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Adres).HasMaxLength(255);
            entity.Property(e => e.DogumYeri)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EvlilikSoyadi)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Il)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Ilce)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Soyad)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TckimlikNo)
                .HasMaxLength(11)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("TCKimlikNo");
            entity.Property(e => e.Telefon)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UyeNo)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Vefat).HasDefaultValue(false);
        });

        modelBuilder.Entity<ZiyaretciSayaci>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ziyaretc__3214EC27B319DD08");

            entity.ToTable("ZiyaretciSayaci");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ToplamZiyaretci).HasDefaultValue(0L);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
