using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Client.Migrations
{
    /// <inheritdoc />
    public partial class AddProductNameToSaleItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "SaleItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "SaleItems");
        }
    }
}
