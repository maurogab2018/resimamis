using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class PushSuscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PUSHSUSCRIPCION",
                columns: table => new
                {
                    idPushSuscripcion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idVoluntaria = table.Column<int>(type: "integer", nullable: false),
                    endpoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    p256dh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    auth = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fechaAlta = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PUSHSUSCRIPCION", x => x.idPushSuscripcion);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PUSHSUSCRIPCION_endpoint",
                table: "PUSHSUSCRIPCION",
                column: "endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PUSHSUSCRIPCION_idVoluntaria",
                table: "PUSHSUSCRIPCION",
                column: "idVoluntaria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PUSHSUSCRIPCION");
        }
    }
}
