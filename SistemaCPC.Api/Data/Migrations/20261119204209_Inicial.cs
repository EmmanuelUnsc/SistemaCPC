using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaCPC.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bitacora",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ValorAnterior = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ValorNuevo = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bitacora", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parametros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AporteMensual = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MontoPorDefensa = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    UmbralInactividad = table.Column<int>(type: "int", nullable: false),
                    TopeDefensasMes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Registro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContrasenaCifrada = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Carnet = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaAfiliacion = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaInicioCobro = table.Column<DateOnly>(type: "date", nullable: true),
                    EsTribunal = table.Column<bool>(type: "bit", nullable: true),
                    FechaBaja = table.Column<DateOnly>(type: "date", nullable: true),
                    MotivoBaja = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Cargo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ajustes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    AfiliadoId = table.Column<int>(type: "int", nullable: false),
                    SuperadministradorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ajustes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ajustes_Usuarios_AfiliadoId",
                        column: x => x.AfiliadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ajustes_Usuarios_SuperadministradorId",
                        column: x => x.SuperadministradorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Defensas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroOficio = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Universidad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Carta = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AfiliadoId = table.Column<int>(type: "int", nullable: false)
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
                name: "FotosAfiliados",
                columns: table => new
                {
                    AfiliadoId = table.Column<int>(type: "int", nullable: false),
                    Datos = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroRecibo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MesesCubiertos = table.Column<int>(type: "int", nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AfiliadoId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Ajustes_AfiliadoId",
                table: "Ajustes",
                column: "AfiliadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ajustes_SuperadministradorId",
                table: "Ajustes",
                column: "SuperadministradorId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Registro",
                table: "Usuarios",
                column: "Registro",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ajustes");

            migrationBuilder.DropTable(
                name: "Bitacora");

            migrationBuilder.DropTable(
                name: "Defensas");

            migrationBuilder.DropTable(
                name: "FotosAfiliados");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Parametros");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
