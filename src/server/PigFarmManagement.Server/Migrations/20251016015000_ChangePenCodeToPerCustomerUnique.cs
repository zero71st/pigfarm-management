using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigFarmManagement.Server.Migrations
{
    /// <inheritdoc />
    public partial class ChangePenCodeToPerCustomerUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing unique index on PenCode
            migrationBuilder.DropIndex(
                name: "IX_PigPens_PenCode",
                table: "PigPens");

            // Create a new composite unique index on (CustomerId, PenCode)
            migrationBuilder.CreateIndex(
                name: "IX_PigPens_CustomerId_PenCode",
                table: "PigPens",
                columns: new[] { "CustomerId", "PenCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the composite unique index
            migrationBuilder.DropIndex(
                name: "IX_PigPens_CustomerId_PenCode",
                table: "PigPens");

            // Recreate the original unique index on PenCode
            migrationBuilder.CreateIndex(
                name: "IX_PigPens_PenCode",
                table: "PigPens",
                column: "PenCode",
                unique: true);
        }
    }
}