using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekYonetim.Migrations
{
    /// <inheritdoc />
    public partial class AddPanelistPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PanelistFotografYolu",
                table: "Haberler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PanelistFotografYolu",
                table: "Haberler");
        }
    }
}
