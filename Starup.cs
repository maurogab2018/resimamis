using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ResimamisBackend.Entidades;
using System.Text;
using System.Text.Json;

namespace ResimamisBackend
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public void ConfigureServicies(IServiceCollection services)
        {

            services.AddCors(options =>
            {
                options.AddPolicy("AllowOrigin", builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin()
                );
            });


            services.AddAuthorization();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    //ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("b5c3a9d1e8f0d2c4b9e1f8a0d2c4b9e1f8a0d2c4b9e1f8a0d2c4b9e1f8a0d2c"))
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var body = JsonSerializer.Serialize(new ApiResponse
                        {
                            message = "No autenticado.",
                            errors = new List<string> { "No autenticado." }
                        });
                        await context.Response.WriteAsync(body);
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        var body = JsonSerializer.Serialize(new ApiResponse
                        {
                            message = "No autorizado.",
                            errors = new List<string> { "No autorizado." }
                        });
                        await context.Response.WriteAsync(body);
                    }
                };
            });

            //referido a inyeccion de dependencias con scoped, singleton
            //services.AddScoped<MIServicio>();

            //services.AddDbContext<ApplicationDbContext>(options =>
            //options.UseSqlServer(this.Configuration.GetConnectionString("DefaultConnection")));
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            app.UseCors("AllowOrigin");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
