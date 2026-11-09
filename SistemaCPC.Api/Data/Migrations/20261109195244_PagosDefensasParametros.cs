using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaCPC.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PagosDefensasParametros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Carnet",
                table: "Usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsTribunal",
                table: "Usuarios",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaAfiliacion",
                table: "Usuarios",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaBaja",
                table: "Usuarios",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaInicioCobro",
                table: "Usuarios",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoBaja",
                table: "Usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Defensas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroOficio = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Universidad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Carta = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    AfiliadoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Defensas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Defensas_Usuarios_AfiliadoId",
                        column: x => x.AfiliadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroRecibo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric", nullable: false),
                    MesesCubiertos = table.Column<int>(type: "integer", nullable: false),
                    Origen = table.Column<string>(type: "text", nullable: false),
                    AfiliadoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Usuarios_AfiliadoId",
                        column: x => x.AfiliadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parametros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AporteMensual = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoPorDefensa = table.Column<decimal>(type: "numeric", nullable: false),
                    UmbralInactividad = table.Column<int>(type: "integer", nullable: false),
                    TopeDefensasMes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametros", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Defensas_AfiliadoId",
                table: "Defensas",
                column: "AfiliadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_AfiliadoId",
                table: "Pagos",
                column: "AfiliadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_NumeroRecibo",
                table: "Pagos",
                column: "NumeroRecibo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Defensas");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Parametros");

            migrationBuilder.DropColumn(
                name: "Carnet",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EsTribunal",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaAfiliacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaBaja",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaInicioCobro",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MotivoBaja",
                table: "Usuarios");
        }
    }
}
