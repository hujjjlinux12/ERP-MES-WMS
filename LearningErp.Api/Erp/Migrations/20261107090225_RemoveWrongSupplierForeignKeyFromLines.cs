using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWrongSupplierForeignKeyFromLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_Suppliers_MaterialCode",
                table: "PurchaseOrderLines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_Suppliers_MaterialCode",
                table: "PurchaseOrderLines",
                column: "MaterialCode",
                principalTable: "Suppliers",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
