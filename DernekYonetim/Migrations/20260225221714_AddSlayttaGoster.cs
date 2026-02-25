using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekYonetim.Migrations
{
    public partial class AddSlayttaGoster : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sadece Haberler tablosuna SlayttaGoster kolonunu ekliyoruz
            migrationBuilder.AddColumn<bool>(
                name: "SlayttaGoster",
                table: "Haberler",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // İşlemi geri almak istersek bu kolonu silmesini söylüyoruz
            migrationBuilder.DropColumn(
                name: "SlayttaGoster",
                table: "Haberler");
        }
    }
}