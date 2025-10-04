using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigFarmManagement.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameInvoiceNumberToTransactionCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InvoiceNumber",
                table: "Feeds",
                newName: "TransactionCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TransactionCode",
                table: "Feeds",
                newName: "InvoiceNumber");
        }
    }
}
