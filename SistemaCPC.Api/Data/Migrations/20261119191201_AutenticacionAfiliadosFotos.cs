using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaCPC.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AutenticacionAfiliadosFotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FotosAfiliados",
                columns: table => new
                {
                    AfiliadoId = table.Column<int>(type: "integer", nullable: false),
                    Datos = table.Column<byte[]>(type: "bytea", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosAfiliados", x => x.AfiliadoId);
                    table.ForeignKey(
                        name: "FK_FotosAfiliados_Usuarios_AfiliadoId",
                        column: x => x.AfiliadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosAfiliados");
        }
    }
}
