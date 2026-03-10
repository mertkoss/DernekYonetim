using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekYonetim.Migrations
{
    /// <inheritdoc />
    public partial class AddGalleryAlbumAndPhotoTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "Galeri");

            //migrationBuilder.AddColumn<string>(
            //    name: "Rol",
            //    table: "AdminKullanicilar",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.CreateTable(
            //    name: "GaleriAlbumleri",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        Baslik = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        KapakFotografYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_GaleriAlbumleri", x => x.Id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "GaleriFotograflari",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        AlbumId = table.Column<int>(type: "int", nullable: false),
            //        Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        FotografYolu = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        YuklemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_GaleriFotograflari", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_GaleriFotograflari_GaleriAlbumleri_AlbumId",
            //            column: x => x.AlbumId,
            //            principalTable: "GaleriAlbumleri",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_GaleriFotograflari_AlbumId",
            //    table: "GaleriFotograflari",
            //    column: "AlbumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GaleriFotograflari");

            migrationBuilder.DropTable(
                name: "GaleriAlbumleri");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "AdminKullanicilar");

            migrationBuilder.CreateTable(
                name: "Galeri",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Baslik = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true),
                    FotografYolu = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    YuklemeTarihi = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Galeri__3214EC272F64344B", x => x.ID);
                });
        }
    }
}
