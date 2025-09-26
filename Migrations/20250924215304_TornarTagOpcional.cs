using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tracking_code_api.Migrations
{
    /// <inheritdoc />
    public partial class TornarTagOpcional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MOTO_TAG_codigo_tag",
                table: "MOTO");

            migrationBuilder.DropIndex(
                name: "IX_MOTO_codigo_tag",
                table: "MOTO");

            migrationBuilder.AlterColumn<string>(
                name: "codigo_tag",
                table: "MOTO",
                type: "NVARCHAR2(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)");

            migrationBuilder.CreateIndex(
                name: "IX_MOTO_codigo_tag",
                table: "MOTO",
                column: "codigo_tag",
                unique: true,
                filter: "\"codigo_tag\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_MOTO_TAG_codigo_tag",
                table: "MOTO",
                column: "codigo_tag",
                principalTable: "TAG",
                principalColumn: "codigo_tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MOTO_TAG_codigo_tag",
                table: "MOTO");

            migrationBuilder.DropIndex(
                name: "IX_MOTO_codigo_tag",
                table: "MOTO");

            migrationBuilder.AlterColumn<string>(
                name: "codigo_tag",
                table: "MOTO",
                type: "NVARCHAR2(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MOTO_codigo_tag",
                table: "MOTO",
                column: "codigo_tag",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MOTO_TAG_codigo_tag",
                table: "MOTO",
                column: "codigo_tag",
                principalTable: "TAG",
                principalColumn: "codigo_tag",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
