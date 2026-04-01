using Microsoft.EntityFrameworkCore;

namespace ResimamisBackend.Datos
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        // Constructor sin DI: usa variables de entorno (Render) o config para armar la conexión.
        public ApplicationDbContext() : base(GetDbContextOptions())
        {
        }

        private static DbContextOptions<ApplicationDbContext> GetDbContextOptions()
        {
            var connectionString = ConnectionStringResolver.Resolve();
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .Options;
        }

        public DbSet<BEBE> BEBE { get; set; }

        public DbSet<MADRE> MADRE { get; set; }

        public DbSet<USUARIO> USUARIO { get; set; }

        public DbSet<VOLUNTARIA> VOLUNTARIA { get; set; }
        public DbSet<DIA> DIA { get; set; }

        public DbSet<HORARIO> HORARIO { get; set; }
        public DbSet<ASISTENCIA> ASISTENCIA { get; set; }
        public DbSet<VOLUNTARIAHORARIO> VOLUNTARIAHORARIO { get; set; }

        public DbSet<ESTADO> ESTADO { get; set; }

        public DbSet<MOVIMIENTOSTOCK> MOVIMIENTOSTOCK { get; set; }

        public DbSet<AMBITO> AMBITO { get; set; }

        public DbSet<ASIGNACION> ASIGNACION { get; set; }

        public DbSet<LOCALIDAD> LOCALIDAD { get; set; }

        public DbSet<INSUMO> INSUMO { get; set; }

        public DbSet<ROL> ROL { get; set; }

        public DbSet<SALA> SALA { get; set; }

        public DbSet<DETALLEASIGNACION> DETALLEASIGNACION { get; set; }

        public DbSet<PROVEEDOR> PROVEEDOR { get; set; }

        public DbSet<TAREA> TAREA { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configura tus entidades y relaciones aquí
            //modelBuilder.Entity<Usuario>().HasMany(u => u.Productos).WithOne(p => p.Usuario);
            modelBuilder.Entity<BEBE>()
            .HasOne(bebe => bebe.Madre)
            .WithMany(madre => madre.Bebe)
            .HasForeignKey(bebe => bebe.IdMadre);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BEBE>()
            .HasOne(bebe => bebe.Estado)
            .WithMany(estado => estado.Bebes)
            .HasForeignKey(bebe => bebe.IdEstado);
            base.OnModelCreating(modelBuilder);



            modelBuilder.Entity<VOLUNTARIA>()
            .HasOne(voluntaria => voluntaria.Estado)
            .WithMany(estado => estado.Voluntarias)
            .HasForeignKey(voluntaria => voluntaria.IdEstado);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VOLUNTARIA>()
            .HasOne(voluntaria => voluntaria.RolInfo)
            .WithMany(rol => rol.Voluntarias)
            .HasForeignKey(voluntaria => voluntaria.IdRol);


            modelBuilder.Entity<BEBE>()
                .HasOne(b => b.Sala)
                .WithMany()
                .HasForeignKey(b => b.IdSala);

            modelBuilder.Entity<ASISTENCIA>()
            .HasOne(a => a.Voluntaria)
            .WithMany(v => v.Asistencias)
            .HasForeignKey(a => a.IdVoluntaria);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ASIGNACION>()
            .HasOne(a => a.voluntaria)
            .WithMany(v => v.Asignaciones)
            .HasForeignKey(a => a.idVoluntaria);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ASIGNACION>()
            .HasOne(a => a.bebe)
            .WithMany(b => b.Asignaciones)
            .HasForeignKey(a => a.idBebe);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ASIGNACION>()
            .HasOne(a => a.tarea)
            .WithMany(b => b.Asignaciones)
            .HasForeignKey(a => a.idTarea);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ASIGNACION>()
            .HasOne(asig => asig.estado)
            .WithMany(estado => estado.Asignaciones)
            .HasForeignKey(asig => asig.idEstado);
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<ESTADO>()
            .HasOne(estado => estado.ambito)
            .WithMany(ambito => ambito.estados)
            .HasForeignKey(estado => estado.idAmbito);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ASIGNACION>()
            .HasMany(a => a.detallesAsignacion)
            .WithOne(d => d.asignacion)
            .HasForeignKey(d => d.idAsignacion);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DETALLEASIGNACION>()
            .HasOne(detalle => detalle.insumo)
            .WithMany(insumo=>insumo.detalles)
            .HasForeignKey(detalle => detalle.idInsumo);
            base.OnModelCreating(modelBuilder);
        }
    }
}
