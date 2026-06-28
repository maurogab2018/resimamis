using Microsoft.Extensions.DependencyInjection;
using ResimamisBackend.Datos;
using ResimamisBackend.Datos.Interfaces;
using ResimamisBackend.Negocio;
using ResimamisBackend.Negocio.Interfaces;

namespace ResimamisBackend.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResimamisServices(this IServiceCollection services)
    {
        // Repositorios (Estado primero; luego sin deps anidadas; luego con deps)
        services.AddScoped<IEstadoRepositorio, EstadoRepositorio>();
        services.AddScoped<IGenericosRepositorio, GenericosRepositorio>();
        services.AddScoped<IProveedorRepositorio, ProveedorRepositorio>();
        services.AddScoped<IHorarioRepositorio, HorarioRepositorio>();
        services.AddScoped<ISalaRepositorio, SalaRepositorio>();
        services.AddScoped<ITareaRepositorio, TareaRepositorio>();
        services.AddScoped<IMadreRepositorio, MadreRepositorio>();
        services.AddScoped<IVoluntariaRepositorio, VoluntariaRepositorio>();
        services.AddScoped<IBebeRepositorio, BebeRepositorio>();
        services.AddScoped<IAsistenciaRepositorio, AsistenciaRepositorio>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<IInsumoRepositorio, InsumoRepositorio>();
        services.AddScoped<IAsignacionRepositorio, AsignacionRepositorio>();
        services.AddScoped<IVisitaRepositorio, VisitaRepositorio>();
        services.AddScoped<IDashboardRepositorio, DashboardRepositorio>();

        // Servicios de negocio
        services.AddScoped<INegGenericos, NegGenericos>();
        services.AddScoped<INegProveedores, NegProveedores>();
        services.AddScoped<INegSalas, NegSalas>();
        services.AddScoped<INegTareas, NegTareas>();
        services.AddScoped<INegUsuarios, NegUsuarios>();
        services.AddScoped<INegBebes, NegBebes>();
        services.AddScoped<INegMadres, NegMadres>();
        services.AddScoped<INegHorariosVoluntaria, NegHorariosVoluntaria>();
        services.AddScoped<INegAsistencia, NegAsistencia>();
        services.AddScoped<INegVoluntaria, NegVoluntaria>();
        services.AddScoped<INegVisitas, NegVisitas>();
        services.AddScoped<INegInsumos, NegInsumos>();
        services.AddScoped<INegAsignacion, NegAsignacion>();
        services.AddScoped<INegEnvioMail, NegEnvioMail>();
        services.AddScoped<INegDashboard, NegDashboard>();

        return services;
    }
}
