using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigFarmManagement.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPosTotalPriceIncludeDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                table: "Feeds",
                newName: "TotalPriceIncludeDiscount");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Feeds",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostDiscountPrice",
                table: "Feeds",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Pos_TotalPriceIncludeDiscount",
                table: "Feeds",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceIncludeDiscount",
                table: "Feeds",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Sys_TotalPriceIncludeDiscount",
                table: "Feeds",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Feeds");

            migrationBuilder.DropColumn(
                name: "CostDiscountPrice",
                table: "Feeds");

            migrationBuilder.DropColumn(
                name: "Pos_TotalPriceIncludeDiscount",
                table: "Feeds");

            migrationBuilder.DropColumn(
                name: "PriceIncludeDiscount",
                table: "Feeds");

            migrationBuilder.DropColumn(
                name: "Sys_TotalPriceIncludeDiscount",
                table: "Feeds");

            migrationBuilder.RenameColumn(
                name: "TotalPriceIncludeDiscount",
                table: "Feeds",
                newName: "TotalPrice");
        }
    }
}
