using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekYonetim.Migrations
{
    /// <inheritdoc />
    public partial class uyeler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IstifaEtti",
                table: "Uyeler",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "IstifaTarihi",
                table: "Uyeler",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "OnayTarihi",
                table: "Uyeler",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bolum",
                table: "EgitimMeslek",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lise",
                table: "EgitimMeslek",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LiseMezuniyetYili",
                table: "EgitimMeslek",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IstifaEtti",
                table: "Uyeler");

            migrationBuilder.DropColumn(
                name: "IstifaTarihi",
                table: "Uyeler");

            migrationBuilder.DropColumn(
                name: "OnayTarihi",
                table: "Uyeler");

            migrationBuilder.DropColumn(
                name: "Bolum",
                table: "EgitimMeslek");

            migrationBuilder.DropColumn(
                name: "Lise",
                table: "EgitimMeslek");

            migrationBuilder.DropColumn(
                name: "LiseMezuniyetYili",
                table: "EgitimMeslek");
        }
    }
}
