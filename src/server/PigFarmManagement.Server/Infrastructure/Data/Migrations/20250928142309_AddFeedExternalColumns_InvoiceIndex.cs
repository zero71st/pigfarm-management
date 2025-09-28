using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigFarmManagement.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedExternalColumns_InvoiceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Feeds_ExternalReference",
                table: "Feeds");

            migrationBuilder.AddColumn<string>(
                name: "ExternalProductCode",
                table: "Feeds",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProductName",
                table: "Feeds",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceReferenceCode",
                table: "Feeds",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UnmappedProduct",
                table: "Feeds",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalProductCode",
                table: "Feeds");

            migrationBuilder.DropColumn(
                name: "ExternalProductName",
                table: "Feeds");

            migrationBuilder.DropColumn(
                name: "InvoiceReferenceCode",
                table: "Feeds");

            migrationBuilder.DropColumn(
                name: "UnmappedProduct",
                table: "Feeds");

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_ExternalReference",
                table: "Feeds",
                column: "ExternalReference",
                unique: true,
                filter: "[ExternalReference] IS NOT NULL");
        }
    }
}
