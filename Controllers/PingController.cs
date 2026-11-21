using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResimamisBackend.Datos;

namespace ResimamisBackend.Controllers
{
    /// <summary>
    /// Despierta el servicio (Render duerme el plan gratis tras 15 min sin tráfico) y la conexión a
    /// la base. Lo usan el ping programado y el front al abrir la app, así la primera pregunta del
    /// día al asistente no se come el arranque en frío.
    /// </summary>
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class PingController(ApplicationDbContext db) : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            bool baseOk;
            try
            {
                baseOk = db.Database.CanConnect();
            }
            catch
            {
                baseOk = false;
            }

            return Ok(new { ok = true, baseDeDatos = baseOk, hora = DateTime.UtcNow });
        }
    }
}
