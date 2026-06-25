using Microsoft.EntityFrameworkCore;
using ResimamisBackend;
using ResimamisBackend.Datos;
using ResimamisBackend.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.OpenApi.Models;
using System.Text.Json;

// Npgsql 6+: timestamptz solo acepta UTC por defecto; este switch tolera DateTime Local (p. ej. del cliente JSON).
// Los repositorios usan UTC explicito + rango "hoy" Argentina donde hace falta.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var requestTimeoutSeconds = builder.Configuration.GetValue("RequestTimeouts:DefaultSeconds", 120);
var dbCommandTimeoutSeconds = builder.Configuration.GetValue("Database:CommandTimeoutSeconds", 120);

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
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.CommandTimeout(dbCommandTimeoutSeconds)));

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds)
    };
});

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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err =>
                    string.IsNullOrWhiteSpace(err.ErrorMessage)
                        ? err.Exception?.Message ?? "Error de validaci�n"
                        : err.ErrorMessage))
                .Where(msg => !string.IsNullOrWhiteSpace(msg))
                .ToList();

            return new BadRequestObjectResult(new ApiResponse
            {
                message = "Error de validación",
                errors = errors.Count > 0 ? errors : new List<string> { "Error de validación" }
            });
        };
    });

// Pasar la config de los servicios
startup.ConfigureServicies(builder.Services);

var app = builder.Build();

// Aplica migraciones autom?ticamente al arrancar (?til para deploy en Render).
// Si no quer?s que se ejecute en todos los entornos, sete? RUN_MIGRATIONS_ON_STARTUP=false.
if (builder.Configuration["RUN_MIGRATIONS_ON_STARTUP"]?.Equals("false", StringComparison.OrdinalIgnoreCase) != true)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

startup.Configure(app, app.Lifetime);

app.UseRequestTimeouts();

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiResponse
        {
            message = "Error interno del servidor.",
            errors = new List<string> { "Error interno del servidor." }
        }));
    });
});

//app.UseHttpsRedirection();

// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();

// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = string.Empty; // Para que se cargue en la ra?z del sitio
});

// AGREGAR ESTO PARA ENRUTAR
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
