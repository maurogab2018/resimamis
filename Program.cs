using Microsoft.EntityFrameworkCore;
using ResimamisBackend;
using ResimamisBackend.Datos;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Render (y otros PaaS) suele setear PORT. Esto asegura que Kestrel escuche en el puerto correcto.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var startup = new Startup(builder.Configuration);

// Add services to the container.
var connectionString = ConnectionStringResolver.Resolve(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Swagger service
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "My API",
        Description = "A simple example ASP.NET Core Web API"
    });
});

builder.Services.AddControllers();

// Pasar la config de los servicios
startup.ConfigureServicies(builder.Services);

var app = builder.Build();

// Aplica migraciones automáticamente al arrancar (útil para deploy en Render).
// Si no querés que se ejecute en todos los entornos, seteá RUN_MIGRATIONS_ON_STARTUP=false.
if (builder.Configuration["RUN_MIGRATIONS_ON_STARTUP"]?.Equals("false", StringComparison.OrdinalIgnoreCase) != true)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

startup.Configure(app, app.Lifetime);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//app.UseHttpsRedirection();

// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();

// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = string.Empty; // Para que se cargue en la raíz del sitio
});

// AGREGAR ESTO PARA ENRUTAR
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
