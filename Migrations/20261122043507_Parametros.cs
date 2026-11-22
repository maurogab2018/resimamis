using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class Parametros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PARAMETRO",
                columns: table => new
                {
                    clave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    valor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARAMETRO", x => x.clave);
                });

            migrationBuilder.InsertData(
                table: "PARAMETRO",
                columns: new[] { "clave", "descripcion", "valor" },
                values: new object[,]
                {
                    { "Asistente.MaxPreguntasDia", "Preguntas al asistente por usuaria y por día. 0 = sin tope.", "150" },
                    { "Asistente.MaxRegistros", "Preguntas guardadas en ASISTENTEREGISTRO (se borran las más viejas).", "1000" },
                    { "Asistente.MaxRegistrosMalos", "Preguntas calificadas como mal que se guardan aparte del tope general.", "200" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PARAMETRO");
        }
    }
}
