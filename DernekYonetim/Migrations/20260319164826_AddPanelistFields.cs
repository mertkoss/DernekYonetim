using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekYonetim.Migrations
{
    /// <inheritdoc />
    public partial class AddPanelistFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvDosyaYolu",
                table: "Haberler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PanelistOzgecmis",
                table: "Haberler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvDosyaYolu",
                table: "Haberler");

            migrationBuilder.DropColumn(
                name: "PanelistOzgecmis",
                table: "Haberler");
        }
    }
}
