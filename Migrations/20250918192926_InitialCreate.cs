using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tracking_code_api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SETOR",
                columns: table => new
                {
                    id_setor = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    nome = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    descricao = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    coordenadas_limite = table.Column<double>(type: "BINARY_DOUBLE", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SETOR", x => x.id_setor);
                });

            migrationBuilder.CreateTable(
                name: "TAG",
                columns: table => new
                {
                    codigo_tag = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    status = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    data_vinculo = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    chassi = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAG", x => x.codigo_tag);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO",
                columns: table => new
                {
                    id_funcionario = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    nome = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    email = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    senha = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    funcao = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.id_funcionario);
                });

            migrationBuilder.CreateTable(
                name: "LOCALIZACAO",
                columns: table => new
                {
                    id_localizacao = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    x = table.Column<decimal>(type: "DECIMAL(12,8)", precision: 12, scale: 8, nullable: false),
                    y = table.Column<decimal>(type: "DECIMAL(12,8)", precision: 12, scale: 8, nullable: false),
                    codigo_tag = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    id_setor = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    data_registro = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOCALIZACAO", x => x.id_localizacao);
                    table.ForeignKey(
                        name: "FK_LOCALIZACAO_SETOR_id_setor",
                        column: x => x.id_setor,
                        principalTable: "SETOR",
                        principalColumn: "id_setor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LOCALIZACAO_TAG_codigo_tag",
                        column: x => x.codigo_tag,
                        principalTable: "TAG",
                        principalColumn: "codigo_tag",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AUDITORIA_MOVIMENTACAO",
                columns: table => new
                {
                    id_audit = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    id_funcionario = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    tipo_operacao = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    data_operacao = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    valores_novos = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    valores_anteriores = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDITORIA_MOVIMENTACAO", x => x.id_audit);
                    table.ForeignKey(
                        name: "FK_AUDITORIA_MOVIMENTACAO_USUARIO_id_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "USUARIO",
                        principalColumn: "id_funcionario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MOTO",
                columns: table => new
                {
                    chassi = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    placa = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    modelo = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    data_cadastro = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    codigo_tag = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    id_setor = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    id_audit = table.Column<int>(type: "NUMBER(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MOTO", x => x.chassi);
                    table.ForeignKey(
                        name: "FK_MOTO_AUDITORIA_MOVIMENTACAO_id_audit",
                        column: x => x.id_audit,
                        principalTable: "AUDITORIA_MOVIMENTACAO",
                        principalColumn: "id_audit");
                    table.ForeignKey(
                        name: "FK_MOTO_SETOR_id_setor",
                        column: x => x.id_setor,
                        principalTable: "SETOR",
                        principalColumn: "id_setor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MOTO_TAG_codigo_tag",
                        column: x => x.codigo_tag,
                        principalTable: "TAG",
                        principalColumn: "codigo_tag",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AUDITORIA_MOVIMENTACAO_id_funcionario",
                table: "AUDITORIA_MOVIMENTACAO",
                column: "id_funcionario");

            migrationBuilder.CreateIndex(
                name: "IX_LOCALIZACAO_codigo_tag",
                table: "LOCALIZACAO",
                column: "codigo_tag");

            migrationBuilder.CreateIndex(
                name: "IX_LOCALIZACAO_id_setor",
                table: "LOCALIZACAO",
                column: "id_setor");

            migrationBuilder.CreateIndex(
                name: "IX_MOTO_codigo_tag",
                table: "MOTO",
                column: "codigo_tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MOTO_id_audit",
                table: "MOTO",
                column: "id_audit");

            migrationBuilder.CreateIndex(
                name: "IX_MOTO_id_setor",
                table: "MOTO",
                column: "id_setor");

            migrationBuilder.CreateIndex(
                name: "IX_MOTO_placa",
                table: "MOTO",
                column: "placa",
                unique: true,
                filter: "\"placa\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SETOR_nome",
                table: "SETOR",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_email",
                table: "USUARIO",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LOCALIZACAO");

            migrationBuilder.DropTable(
                name: "MOTO");

            migrationBuilder.DropTable(
                name: "AUDITORIA_MOVIMENTACAO");

            migrationBuilder.DropTable(
                name: "SETOR");

            migrationBuilder.DropTable(
                name: "TAG");

            migrationBuilder.DropTable(
                name: "USUARIO");
        }
    }
}
