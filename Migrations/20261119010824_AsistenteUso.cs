using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class AsistenteUso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ASISTENTEUSO",
                columns: table => new
                {
                    idAsistenteUso = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dni = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    preguntas = table.Column<int>(type: "integer", nullable: false),
                    tokensEntrada = table.Column<int>(type: "integer", nullable: false),
                    tokensSalida = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASISTENTEUSO", x => x.idAsistenteUso);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ASISTENTEUSO_dni_fecha",
                table: "ASISTENTEUSO",
                columns: new[] { "dni", "fecha" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASISTENTEUSO");
        }
    }
}
