using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FarmAppAspire.CustomerService.Migrations
{
    /// <inheritdoc />
    public partial class OrderManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerKey",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CustomerKeyCollision",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CustomerPricings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoxSize = table.Column<string>(type: "text", nullable: false),
                    PricePerLb = table.Column<decimal>(type: "numeric", nullable: false),
                    ShippingRate = table.Column<decimal>(type: "numeric", nullable: true),
                    MinQty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPricings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPricings_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FedExTierConfigs",
                columns: table => new
                {
                    TierSize = table.Column<string>(type: "text", nullable: false),
                    WeightOz = table.Column<decimal>(type: "numeric", nullable: false),
                    FixedPrice = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FedExTierConfigs", x => x.TierSize);
                });

            migrationBuilder.CreateTable(
                name: "InsulatedBoxConfigs",
                columns: table => new
                {
                    Size = table.Column<string>(type: "text", nullable: false),
                    WeightLbs = table.Column<decimal>(type: "numeric", nullable: false),
                    BasePricePerLb = table.Column<decimal>(type: "numeric", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsulatedBoxConfigs", x => x.Size);
                });

            migrationBuilder.CreateTable(
                name: "StandingOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Frequency = table.Column<string>(type: "text", nullable: false),
                    MonthlyWeek = table.Column<string>(type: "text", nullable: true),
                    IsSample = table.Column<bool>(type: "boolean", nullable: false),
                    SeasonYear = table.Column<int>(type: "integer", nullable: false),
                    StartWeek = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandingOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandingOrders_CustomerContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CustomerContacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StandingOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandingOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    WeekOf = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShipDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSample = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderInstances_CustomerContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CustomerContacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderInstances_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderInstances_StandingOrders_StandingOrderId",
                        column: x => x.StandingOrderId,
                        principalTable: "StandingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StandingOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandingOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoxSize = table.Column<string>(type: "text", nullable: false),
                    Qty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandingOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandingOrderLines_StandingOrders_StandingOrderId",
                        column: x => x.StandingOrderId,
                        principalTable: "StandingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StandingOrderSkips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandingOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekOf = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandingOrderSkips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandingOrderSkips_StandingOrders_StandingOrderId",
                        column: x => x.StandingOrderId,
                        principalTable: "StandingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    SeasonYear = table.Column<int>(type: "integer", nullable: false),
                    SeekNum = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invoices_OrderInstances_OrderInstanceId",
                        column: x => x.OrderInstanceId,
                        principalTable: "OrderInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderInstanceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoxSize = table.Column<string>(type: "text", nullable: true),
                    EffectivePricePerLb = table.Column<decimal>(type: "numeric", nullable: true),
                    FedExTierSize = table.Column<string>(type: "text", nullable: true),
                    FedExFixedPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    PackagingType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderInstanceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderInstanceLines_OrderInstances_OrderInstanceId",
                        column: x => x.OrderInstanceId,
                        principalTable: "OrderInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FedExTierConfigs",
                columns: new[] { "TierSize", "FixedPrice", "WeightOz" },
                values: new object[,]
                {
                    { "EightOz", 19.99m, 8m },
                    { "FiveLb", 99.99m, 80m },
                    { "FourOz", 12.99m, 4m },
                    { "OneLb", 27.99m, 16m },
                    { "OneOz", 5.99m, 1m },
                    { "ThreeLb", 68.99m, 48m },
                    { "TwoLb", 42.99m, 32m },
                    { "TwoOz", 8.99m, 2m }
                });

            migrationBuilder.InsertData(
                table: "InsulatedBoxConfigs",
                columns: new[] { "Size", "BasePricePerLb", "IsDefault", "WeightLbs" },
                values: new object[,]
                {
                    { "FiveLb", 13m, false, 5m },
                    { "TenLb", 13m, false, 10m },
                    { "TwelveLb", 13m, true, 12m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricings_CustomerId_BoxSize",
                table: "CustomerPricings",
                columns: new[] { "CustomerId", "BoxSize" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId_SeasonYear_Channel_SeekNum",
                table: "Invoices",
                columns: new[] { "CustomerId", "SeasonYear", "Channel", "SeekNum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Label",
                table: "Invoices",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderInstanceId",
                table: "Invoices",
                column: "OrderInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderInstanceLines_OrderInstanceId",
                table: "OrderInstanceLines",
                column: "OrderInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInstances_ContactId",
                table: "OrderInstances",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInstances_CustomerId_WeekOf_Status",
                table: "OrderInstances",
                columns: new[] { "CustomerId", "WeekOf", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderInstances_StandingOrderId",
                table: "OrderInstances",
                column: "StandingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StandingOrderLines_StandingOrderId",
                table: "StandingOrderLines",
                column: "StandingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StandingOrders_ContactId",
                table: "StandingOrders",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_StandingOrders_CustomerId_Status",
                table: "StandingOrders",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StandingOrderSkips_StandingOrderId_WeekOf",
                table: "StandingOrderSkips",
                columns: new[] { "StandingOrderId", "WeekOf" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPricings");

            migrationBuilder.DropTable(
                name: "FedExTierConfigs");

            migrationBuilder.DropTable(
                name: "InsulatedBoxConfigs");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "OrderInstanceLines");

            migrationBuilder.DropTable(
                name: "StandingOrderLines");

            migrationBuilder.DropTable(
                name: "StandingOrderSkips");

            migrationBuilder.DropTable(
                name: "OrderInstances");

            migrationBuilder.DropTable(
                name: "StandingOrders");

            migrationBuilder.DropColumn(
                name: "CustomerKey",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerKeyCollision",
                table: "Customers");
        }
    }
}
