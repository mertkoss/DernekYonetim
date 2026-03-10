using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekYonetim.Migrations
{
    /// <inheritdoc />
    public partial class TablolariZorlaKur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Eski Galeri tablosunun kalıntıları varsa veritabanından tamamen temizle
            migrationBuilder.Sql("IF OBJECT_ID('Galeri', 'U') IS NOT NULL DROP TABLE Galeri;");

            // 2. Yeni Albüm Tablosunu Zorla Kur
            migrationBuilder.CreateTable(
                name: "GaleriAlbumleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Baslik = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KapakFotografYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GaleriAlbumleri", x => x.Id);
                });

            // 3. Yeni Fotoğraflar Tablosunu Zorla Kur
            migrationBuilder.CreateTable(
                name: "GaleriFotograflari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlbumId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotografYolu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YuklemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GaleriFotograflari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GaleriFotograflari_GaleriAlbumleri_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "GaleriAlbumleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 4. Tablolar Arası İlişkiyi Bağla
            migrationBuilder.CreateIndex(
                name: "IX_GaleriFotograflari_AlbumId",
                table: "GaleriFotograflari",
                column: "AlbumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
