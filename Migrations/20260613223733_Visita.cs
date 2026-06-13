using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class Visita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VISITA",
                columns: table => new
                {
                    idVisita = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idBebe = table.Column<int>(type: "integer", nullable: false),
                    nombreVisitante = table.Column<string>(type: "text", nullable: false),
                    familiar = table.Column<string>(type: "text", nullable: false),
                    fechaHoraVisita = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    observacion = table.Column<string>(type: "text", nullable: true),
                    documentoVisitante = table.Column<int>(type: "integer", nullable: true),
                    telefonoVisitante = table.Column<long>(type: "bigint", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    fechaRegistro = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VISITA", x => x.idVisita);
                    table.ForeignKey(
                        name: "FK_VISITA_BEBE_idBebe",
                        column: x => x.idBebe,
                        principalTable: "BEBE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VISITA_idBebe",
                table: "VISITA",
                column: "idBebe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VISITA");
        }
    }
}
