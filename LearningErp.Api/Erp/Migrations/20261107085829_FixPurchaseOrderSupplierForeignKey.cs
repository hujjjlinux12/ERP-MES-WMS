using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class FixPurchaseOrderSupplierForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierCode",
                table: "PurchaseOrders",
                column: "SupplierCode");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierCode",
                table: "PurchaseOrders",
                column: "SupplierCode",
                principalTable: "Suppliers",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierCode",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SupplierCode",
                table: "PurchaseOrders");
        }
    }
}
