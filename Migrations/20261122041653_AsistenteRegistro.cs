using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class AsistenteRegistro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ASISTENTEREGISTRO",
                columns: table => new
                {
                    idAsistenteRegistro = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    dni = table.Column<int>(type: "integer", nullable: false),
                    pregunta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    respuesta = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    herramientas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    resultado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    duracionMs = table.Column<int>(type: "integer", nullable: false),
                    tokens = table.Column<int>(type: "integer", nullable: false),
                    calificacion = table.Column<short>(type: "smallint", nullable: true),
                    comentario = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASISTENTEREGISTRO", x => x.idAsistenteRegistro);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASISTENTEREGISTRO");
        }
    }
}
