using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ResimamisBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AMBITO",
                columns: table => new
                {
                    idAmbito = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AMBITO", x => x.idAmbito);
                });

            migrationBuilder.CreateTable(
                name: "DIA",
                columns: table => new
                {
                    IdDia = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descripcion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DIA", x => x.IdDia);
                });

            migrationBuilder.CreateTable(
                name: "HORARIO",
                columns: table => new
                {
                    IdHorario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdDia = table.Column<int>(type: "integer", nullable: false),
                    Turno = table.Column<string>(type: "text", nullable: false),
                    HoraIngreso = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraSalida = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HORARIO", x => x.IdHorario);
                });

            migrationBuilder.CreateTable(
                name: "INSUMO",
                columns: table => new
                {
                    idInsumo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    stockMaximo = table.Column<int>(type: "integer", nullable: false),
                    stockMinimo = table.Column<int>(type: "integer", nullable: false),
                    stockActual = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INSUMO", x => x.idInsumo);
                });

            migrationBuilder.CreateTable(
                name: "LOCALIDAD",
                columns: table => new
                {
                    idLocalidad = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    idProvincia = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOCALIDAD", x => x.idLocalidad);
                });

            migrationBuilder.CreateTable(
                name: "MADRE",
                columns: table => new
                {
                    IdMadre = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Apellido = table.Column<string>(type: "text", nullable: false),
                    Dni = table.Column<int>(type: "integer", nullable: false),
                    Localidad = table.Column<int>(type: "integer", nullable: false),
                    EstadoCivil = table.Column<int>(type: "integer", nullable: false),
                    CantidadHijos = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    MotivoAbrazo = table.Column<string>(type: "text", nullable: false),
                    Celular = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MADRE", x => x.IdMadre);
                });

            migrationBuilder.CreateTable(
                name: "MOVIMIENTOSTOCK",
                columns: table => new
                {
                    idMovimiento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idInsumo = table.Column<int>(type: "integer", nullable: false),
                    idBebe = table.Column<int>(type: "integer", nullable: true),
                    idVoluntaria = table.Column<int>(type: "integer", nullable: true),
                    fechaMovimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    observacion = table.Column<string>(type: "text", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: true),
                    esEntrada = table.Column<string>(type: "text", nullable: false),
                    idProveedor = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MOVIMIENTOSTOCK", x => x.idMovimiento);
                });

            migrationBuilder.CreateTable(
                name: "PROVEEDOR",
                columns: table => new
                {
                    idProveedor = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROVEEDOR", x => x.idProveedor);
                });

            migrationBuilder.CreateTable(
                name: "ROL",
                columns: table => new
                {
                    IdRol = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROL", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "SALA",
                columns: table => new
                {
                    IdSala = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SALA", x => x.IdSala);
                });

            migrationBuilder.CreateTable(
                name: "TAREA",
                columns: table => new
                {
                    idTarea = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAREA", x => x.idTarea);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Dni = table.Column<int>(type: "integer", nullable: false),
                    Contrasena = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdVoluntaria = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "VOLUNTARIAHORARIO",
                columns: table => new
                {
                    IdHorarioVoluntaria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdHorario = table.Column<int>(type: "integer", nullable: false),
                    IdVoluntaria = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VOLUNTARIAHORARIO", x => x.IdHorarioVoluntaria);
                });

            migrationBuilder.CreateTable(
                name: "ESTADO",
                columns: table => new
                {
                    idEstado = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    idAmbito = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESTADO", x => x.idEstado);
                    table.ForeignKey(
                        name: "FK_ESTADO_AMBITO_idAmbito",
                        column: x => x.idAmbito,
                        principalTable: "AMBITO",
                        principalColumn: "idAmbito",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BEBE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Dni = table.Column<int>(type: "integer", nullable: true),
                    nombre = table.Column<string>(type: "text", nullable: true),
                    apellido = table.Column<string>(type: "text", nullable: true),
                    Sexo = table.Column<string>(type: "text", nullable: true),
                    FechaNacimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LugarNacimiento = table.Column<string>(type: "text", nullable: true),
                    FechaIngresoNEO = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PesoNacimiento = table.Column<decimal>(type: "numeric", nullable: true),
                    PesoIngresoNEO = table.Column<decimal>(type: "numeric", nullable: true),
                    PesoDiaAbrazos = table.Column<decimal>(type: "numeric", nullable: true),
                    PesoAlta = table.Column<decimal>(type: "numeric", nullable: true),
                    DiagnosticoIngreso = table.Column<string>(type: "text", nullable: true),
                    DiagnosticoEgreso = table.Column<string>(type: "text", nullable: true),
                    IdSala = table.Column<int>(type: "integer", nullable: true),
                    IdMadre = table.Column<int>(type: "integer", nullable: true),
                    IdEstado = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BEBE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BEBE_ESTADO_IdEstado",
                        column: x => x.IdEstado,
                        principalTable: "ESTADO",
                        principalColumn: "idEstado");
                    table.ForeignKey(
                        name: "FK_BEBE_MADRE_IdMadre",
                        column: x => x.IdMadre,
                        principalTable: "MADRE",
                        principalColumn: "IdMadre");
                    table.ForeignKey(
                        name: "FK_BEBE_SALA_IdSala",
                        column: x => x.IdSala,
                        principalTable: "SALA",
                        principalColumn: "IdSala");
                });

            migrationBuilder.CreateTable(
                name: "VOLUNTARIA",
                columns: table => new
                {
                    IdVoluntaria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Dni = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Apellido = table.Column<string>(type: "text", nullable: false),
                    Mail = table.Column<string>(type: "text", nullable: false),
                    Celular = table.Column<long>(type: "bigint", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdEstado = table.Column<int>(type: "integer", nullable: true),
                    IdRol = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VOLUNTARIA", x => x.IdVoluntaria);
                    table.ForeignKey(
                        name: "FK_VOLUNTARIA_ESTADO_IdEstado",
                        column: x => x.IdEstado,
                        principalTable: "ESTADO",
                        principalColumn: "idEstado");
                    table.ForeignKey(
                        name: "FK_VOLUNTARIA_ROL_IdRol",
                        column: x => x.IdRol,
                        principalTable: "ROL",
                        principalColumn: "IdRol");
                });

            migrationBuilder.CreateTable(
                name: "ASIGNACION",
                columns: table => new
                {
                    idAsignacion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fechaHoraInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechaHoraFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechaHoraAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    idBebe = table.Column<int>(type: "integer", nullable: true),
                    idTarea = table.Column<int>(type: "integer", nullable: true),
                    idEstado = table.Column<int>(type: "integer", nullable: false),
                    idVoluntaria = table.Column<int>(type: "integer", nullable: false),
                    comentario = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASIGNACION", x => x.idAsignacion);
                    table.ForeignKey(
                        name: "FK_ASIGNACION_BEBE_idBebe",
                        column: x => x.idBebe,
                        principalTable: "BEBE",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ASIGNACION_ESTADO_idEstado",
                        column: x => x.idEstado,
                        principalTable: "ESTADO",
                        principalColumn: "idEstado",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ASIGNACION_TAREA_idTarea",
                        column: x => x.idTarea,
                        principalTable: "TAREA",
                        principalColumn: "idTarea");
                    table.ForeignKey(
                        name: "FK_ASIGNACION_VOLUNTARIA_idVoluntaria",
                        column: x => x.idVoluntaria,
                        principalTable: "VOLUNTARIA",
                        principalColumn: "IdVoluntaria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ASISTENCIA",
                columns: table => new
                {
                    IdAsistencia = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdVoluntaria = table.Column<int>(type: "integer", nullable: true),
                    IdHorario = table.Column<int>(type: "integer", nullable: true),
                    FechaHoraSalida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaHoraIngreso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASISTENCIA", x => x.IdAsistencia);
                    table.ForeignKey(
                        name: "FK_ASISTENCIA_VOLUNTARIA_IdVoluntaria",
                        column: x => x.IdVoluntaria,
                        principalTable: "VOLUNTARIA",
                        principalColumn: "IdVoluntaria");
                });

            migrationBuilder.CreateTable(
                name: "DETALLEASIGNACION",
                columns: table => new
                {
                    idDetalleAsignacion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    fechaRetiro = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    nombreInsumo = table.Column<string>(type: "text", nullable: true),
                    idAsignacion = table.Column<int>(type: "integer", nullable: false),
                    idInsumo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DETALLEASIGNACION", x => x.idDetalleAsignacion);
                    table.ForeignKey(
                        name: "FK_DETALLEASIGNACION_ASIGNACION_idAsignacion",
                        column: x => x.idAsignacion,
                        principalTable: "ASIGNACION",
                        principalColumn: "idAsignacion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DETALLEASIGNACION_INSUMO_idInsumo",
                        column: x => x.idInsumo,
                        principalTable: "INSUMO",
                        principalColumn: "idInsumo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ASIGNACION_idBebe",
                table: "ASIGNACION",
                column: "idBebe");

            migrationBuilder.CreateIndex(
                name: "IX_ASIGNACION_idEstado",
                table: "ASIGNACION",
                column: "idEstado");

            migrationBuilder.CreateIndex(
                name: "IX_ASIGNACION_idTarea",
                table: "ASIGNACION",
                column: "idTarea");

            migrationBuilder.CreateIndex(
                name: "IX_ASIGNACION_idVoluntaria",
                table: "ASIGNACION",
                column: "idVoluntaria");

            migrationBuilder.CreateIndex(
                name: "IX_ASISTENCIA_IdVoluntaria",
                table: "ASISTENCIA",
                column: "IdVoluntaria");

            migrationBuilder.CreateIndex(
                name: "IX_BEBE_IdEstado",
                table: "BEBE",
                column: "IdEstado");

            migrationBuilder.CreateIndex(
                name: "IX_BEBE_IdMadre",
                table: "BEBE",
                column: "IdMadre");

            migrationBuilder.CreateIndex(
                name: "IX_BEBE_IdSala",
                table: "BEBE",
                column: "IdSala");

            migrationBuilder.CreateIndex(
                name: "IX_DETALLEASIGNACION_idAsignacion",
                table: "DETALLEASIGNACION",
                column: "idAsignacion");

            migrationBuilder.CreateIndex(
                name: "IX_DETALLEASIGNACION_idInsumo",
                table: "DETALLEASIGNACION",
                column: "idInsumo");

            migrationBuilder.CreateIndex(
                name: "IX_ESTADO_idAmbito",
                table: "ESTADO",
                column: "idAmbito");

            migrationBuilder.CreateIndex(
                name: "IX_VOLUNTARIA_IdEstado",
                table: "VOLUNTARIA",
                column: "IdEstado");

            migrationBuilder.CreateIndex(
                name: "IX_VOLUNTARIA_IdRol",
                table: "VOLUNTARIA",
                column: "IdRol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASISTENCIA");

            migrationBuilder.DropTable(
                name: "DETALLEASIGNACION");

            migrationBuilder.DropTable(
                name: "DIA");

            migrationBuilder.DropTable(
                name: "HORARIO");

            migrationBuilder.DropTable(
                name: "LOCALIDAD");

            migrationBuilder.DropTable(
                name: "MOVIMIENTOSTOCK");

            migrationBuilder.DropTable(
                name: "PROVEEDOR");

            migrationBuilder.DropTable(
                name: "USUARIO");

            migrationBuilder.DropTable(
                name: "VOLUNTARIAHORARIO");

            migrationBuilder.DropTable(
                name: "ASIGNACION");

            migrationBuilder.DropTable(
                name: "INSUMO");

            migrationBuilder.DropTable(
                name: "BEBE");

            migrationBuilder.DropTable(
                name: "TAREA");

            migrationBuilder.DropTable(
                name: "VOLUNTARIA");

            migrationBuilder.DropTable(
                name: "MADRE");

            migrationBuilder.DropTable(
                name: "SALA");

            migrationBuilder.DropTable(
                name: "ESTADO");

            migrationBuilder.DropTable(
                name: "ROL");

            migrationBuilder.DropTable(
                name: "AMBITO");
        }
    }
}
