using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmAppAspire.CustomerService.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingUsesShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BillingUsesShipping",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingUsesShipping",
                table: "Customers");
        }
    }
}
