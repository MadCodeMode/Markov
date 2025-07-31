using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markov.Services.Migrations
{
    /// <inheritdoc />
    public partial class addedparamstocandleandasset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Close",
                table: "Candles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "High",
                table: "Candles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Low",
                table: "Candles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Open",
                table: "Candles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TradeCount",
                table: "Candles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Volume",
                table: "Candles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "AssetType",
                table: "Assets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Close",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "High",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "Low",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "Open",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "TradeCount",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "AssetType",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Assets");
        }
    }
}
