using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigFarmManagement.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPOSPOSFieldsToFeedFormula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeedFormulas_ProductCode",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "BagPerPig",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "FeedFormulas");

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "FeedFormulas",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "FeedFormulas",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsumeRate",
                table: "FeedFormulas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "FeedFormulas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalId",
                table: "FeedFormulas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "FeedFormulas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "FeedFormulas",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitName",
                table: "FeedFormulas",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedFormulas_Code",
                table: "FeedFormulas",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_FeedFormulas_ExternalId",
                table: "FeedFormulas",
                column: "ExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeedFormulas_Code",
                table: "FeedFormulas");

            migrationBuilder.DropIndex(
                name: "IX_FeedFormulas_ExternalId",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "ConsumeRate",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "FeedFormulas");

            migrationBuilder.DropColumn(
                name: "UnitName",
                table: "FeedFormulas");

            migrationBuilder.AddColumn<decimal>(
                name: "BagPerPig",
                table: "FeedFormulas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "FeedFormulas",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "FeedFormulas",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "FeedFormulas",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FeedFormulas_ProductCode",
                table: "FeedFormulas",
                column: "ProductCode",
                unique: true);
        }
    }
}
