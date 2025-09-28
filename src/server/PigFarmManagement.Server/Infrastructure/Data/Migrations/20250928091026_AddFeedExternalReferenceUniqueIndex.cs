using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigFarmManagement.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedExternalReferenceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: true),
                    KeyCardId = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Sex = table.Column<string>(type: "TEXT", nullable: true),
                    Zipcode = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeedFormulas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BagPerPig = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedFormulas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PigPens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PenCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PigQty = table.Column<int>(type: "INTEGER", nullable: false),
                    RegisterDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActHarvestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstimatedHarvestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FeedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Investment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DepositPerPig = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SelectedBrand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsCalculationLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PigPens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PigPens_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Deposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PigPenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deposits_PigPens_PigPenId",
                        column: x => x.PigPenId,
                        principalTable: "PigPens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PigPenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FeedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExternalReference = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feeds_PigPens_PigPenId",
                        column: x => x.PigPenId,
                        principalTable: "PigPens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Harvests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PigPenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HarvestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PigCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalePricePerKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Harvests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Harvests_PigPens_PigPenId",
                        column: x => x.PigPenId,
                        principalTable: "PigPens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PigPenFormulaAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PigPenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalFormulaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    AssignedPigQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedBagPerPig = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AssignedTotalBags = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockReason = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LockedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PigPenFormulaAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PigPenFormulaAssignments_FeedFormulas_OriginalFormulaId",
                        column: x => x.OriginalFormulaId,
                        principalTable: "FeedFormulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PigPenFormulaAssignments_PigPens_PigPenId",
                        column: x => x.PigPenId,
                        principalTable: "PigPens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Code",
                table: "Customers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_PigPenId",
                table: "Deposits",
                column: "PigPenId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedFormulas_ProductCode",
                table: "FeedFormulas",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_ExternalReference",
                table: "Feeds",
                column: "ExternalReference",
                unique: true,
                filter: "[ExternalReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_PigPenId",
                table: "Feeds",
                column: "PigPenId");

            migrationBuilder.CreateIndex(
                name: "IX_Harvests_PigPenId",
                table: "Harvests",
                column: "PigPenId");

            migrationBuilder.CreateIndex(
                name: "IX_PigPenFormulaAssignments_OriginalFormulaId",
                table: "PigPenFormulaAssignments",
                column: "OriginalFormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_PigPenFormulaAssignments_PigPenId",
                table: "PigPenFormulaAssignments",
                column: "PigPenId");

            migrationBuilder.CreateIndex(
                name: "IX_PigPens_CustomerId",
                table: "PigPens",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PigPens_PenCode",
                table: "PigPens",
                column: "PenCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Deposits");

            migrationBuilder.DropTable(
                name: "Feeds");

            migrationBuilder.DropTable(
                name: "Harvests");

            migrationBuilder.DropTable(
                name: "PigPenFormulaAssignments");

            migrationBuilder.DropTable(
                name: "FeedFormulas");

            migrationBuilder.DropTable(
                name: "PigPens");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
