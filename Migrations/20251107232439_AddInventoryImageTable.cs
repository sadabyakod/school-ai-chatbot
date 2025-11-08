using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ImageAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryImageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inventory_Image",
                columns: table => new
                {
                    ItemNum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Store_ID = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ImageLocation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ID = table.Column<long>(type: "bigint", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory_Image", x => new { x.ItemNum, x.Store_ID, x.ImageLocation });
                });

            migrationBuilder.InsertData(
                table: "Images",
                columns: new[] { "Id", "ContentType", "CreatedDate", "Description", "FileSize", "ImageName", "ImagePath", "ModifiedDate" },
                values: new object[,]
                {
                    { 1, "image/jpeg", new DateTime(2025, 11, 7, 23, 24, 36, 765, DateTimeKind.Utc).AddTicks(7158), "A sample image for testing", 1024000L, "Sample Image 1", "C:\\temp\\sample1.jpg", null },
                    { 2, "image/png", new DateTime(2025, 11, 7, 23, 24, 36, 765, DateTimeKind.Utc).AddTicks(7687), "Another sample image for testing", 2048000L, "Sample Image 2", "C:\\temp\\sample2.png", null }
                });

            migrationBuilder.InsertData(
                table: "Inventory_Image",
                columns: new[] { "ImageLocation", "ItemNum", "Store_ID", "ID", "Position" },
                values: new object[,]
                {
                    { "D:\\school-ai-chatbot\\winform_project\\TestImages\\test-blue.png", "ITEM001", "ST001", 2L, 2 },
                    { "D:\\school-ai-chatbot\\winform_project\\TestImages\\test-red.png", "ITEM001", "ST001", 1L, 1 },
                    { "D:\\school-ai-chatbot\\winform_project\\TestImages\\test-gradient.jpg", "ITEM002", "ST002", 3L, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_CreatedDate",
                table: "Images",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ImageName",
                table: "Images",
                column: "ImageName");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Image_ItemNum",
                table: "Inventory_Image",
                column: "ItemNum");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Image_ItemNum_Store_ID",
                table: "Inventory_Image",
                columns: new[] { "ItemNum", "Store_ID" });

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_Image_Store_ID",
                table: "Inventory_Image",
                column: "Store_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "Inventory_Image");
        }
    }
}
