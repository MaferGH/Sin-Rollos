using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using WebApp.Models.Solicitudes;
using WebApp.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;

namespace WebApp.Controllers
{
    [Authorize]
    public class JefeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ISolicitudService _solicitudService;

        public JefeController(IConfiguration configuration, ISolicitudService solicitudService)
        {
            _configuration = configuration;
            _solicitudService = solicitudService;
        }

        public class SolicitudAccionModel
        {
            public int idSolicitud { get; set; }
            public string motivo { get; set; }
            public string firma { get; set; }
        }


        private IActionResult VerificarRol()
        {
            var rol = HttpContext.Session.GetString("rol");
            if (rol == null || rol != "Jefe")
                return RedirectToAction("Index", "Login");
            return null;
        }

        //obtener el tipo de solicitud de la BD
        private async Task<string> ObtenerTipoSolicitud(int idSolicitud)
        {
            var connectionString = _configuration.GetConnectionString("WebAppContext");
            if (string.IsNullOrEmpty(connectionString)) return null;

            string tipoSolicitud = null;
            var query = "SELECT tipoSolicitud FROM Solicitudes WHERE idSolicitud = @idSolicitud";

            try
            {
                using var connection = new MySqlConnection(connectionString);
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@idSolicitud", idSolicitud);
                await connection.OpenAsync();

                tipoSolicitud = (string)await command.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener tipoSolicitud para ID {idSolicitud}: {ex.Message}");
            }
            // Retorna el tipo de solicitud (ej. "Vacaciones", "Equipo") o null
            return tipoSolicitud;
        }


        //  guardar la firma de Base64 a un archivo .png 
        private void GuardarFirmaBase64(string base64, string nombreArchivo, string rutaDirectorio)
        {
            if (!string.IsNullOrEmpty(base64))
            {
                var data = Regex.Replace(base64, @"^data:image\/[a-zA-Z]+;base64,", string.Empty);

                try
                {
                    byte[] bytes = Convert.FromBase64String(data);
                    var rutaCompleta = Path.Combine(rutaDirectorio, nombreArchivo);

                    if (!Directory.Exists(rutaDirectorio))
                    {
                        Directory.CreateDirectory(rutaDirectorio);
                    }

                    System.IO.File.WriteAllBytes(rutaCompleta, bytes);
                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Error al convertir Base64 para {nombreArchivo}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al guardar el archivo de firma: {ex.Message}");
                }
            }
        }


        [HttpPost]
        public async Task<IActionResult> AprobarSolicitud([FromForm] SolicitudAccionModel model)
        {
            var resultado = VerificarRol();
            if (resultado != null) return Json(new { success = false, message = "Acceso denegado. Por favor, vuelva a iniciar sesión." });
            if (model == null || model.idSolicitud <= 0)
                return Json(new { success = false, message = "Error: ID de solicitud inválido." });
            if (string.IsNullOrEmpty(model.motivo) || string.IsNullOrEmpty(model.firma))
                return Json(new { success = false, message = "Error: El motivo y la firma son obligatorios." });

            try
            {
                // Obtener el tipo de solicitud
                string tipoSolicitud = await ObtenerTipoSolicitud(model.idSolicitud);
                if (string.IsNullOrEmpty(tipoSolicitud))
                {
                    return Json(new { success = false, message = "Error: No se pudo determinar el tipo de solicitud para guardar la firma." });
                }

                bool success = await _solicitudService.AprobarSolicitud(model.idSolicitud, model.motivo, model.firma);
                if (!success)
                {
                    return Json(new { success = false, message = $"Error: No se pudo actualizar la solicitud #{model.idSolicitud} en la base de datos." });
                }

                string rutaBase = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "Archivos",
                    tipoSolicitud,
                    model.idSolicitud.ToString()
                );

                GuardarFirmaBase64(model.firma, "Firma_Jefe.png", rutaBase);


                return Json(new { success = true, message = $"Solicitud #{model.idSolicitud} APROBADA con éxito.", id = model.idSolicitud });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al aprobar la solicitud #{model.idSolicitud}: {ex.Message}");
                return Json(new { success = false, message = $"Error al aprobar la solicitud: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RechazarSolicitud([FromForm] SolicitudAccionModel model)
        {
            var resultado = VerificarRol();
            if (resultado != null) return Json(new { success = false, message = "Acceso denegado. Por favor, vuelva a iniciar sesión." });
            if (model == null || model.idSolicitud <= 0)
                return Json(new { success = false, message = "Error: ID de solicitud inválido." });
            if (string.IsNullOrEmpty(model.motivo) || string.IsNullOrEmpty(model.firma))
                return Json(new { success = false, message = "Error: El motivo y la firma son obligatorios." });

            try
            {
                string tipoSolicitud = await ObtenerTipoSolicitud(model.idSolicitud);
                if (string.IsNullOrEmpty(tipoSolicitud))
                {
                    return Json(new { success = false, message = "Error: No se pudo determinar el tipo de solicitud para guardar la firma." });
                }

                bool success = await _solicitudService.RechazarSolicitud(model.idSolicitud, model.motivo, model.firma);
                if (!success)
                {
                    return Json(new { success = false, message = $"Error: No se pudo actualizar la solicitud #{model.idSolicitud} en la base de datos." });
                }

                string rutaBase = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "Archivos",
                    tipoSolicitud,
                    model.idSolicitud.ToString()
                );

                GuardarFirmaBase64(model.firma, "Firma_Jefe.png", rutaBase);

                return Json(new { success = true, message = $"Solicitud #{model.idSolicitud} RECHAZADA con éxito.", id = model.idSolicitud });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al rechazar la solicitud #{model.idSolicitud}: {ex.Message}");
                return Json(new { success = false, message = $"Error al rechazar la solicitud: {ex.Message}" });
            }
        }




        public async Task<IActionResult> Index()
        {
            var rol = HttpContext.Session.GetString("rol");
            var departamentoJefe = HttpContext.Session.GetString("Departamento");

            if (rol == null)
            {
                return RedirectToAction("Index", "Login");
            }
            if (rol != "Jefe")
                return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(departamentoJefe))
            {
                return RedirectToAction("Index", "Login");
            }

            JefeDashboardViewModel dashboardData;
            try
            {
                dashboardData = await _solicitudService.GetDashboardMetricsByDepartamentoAsync(departamentoJefe);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener métricas del dashboard para depto {departamentoJefe}: {ex.Message}");
                dashboardData = new JefeDashboardViewModel();
            }
            return View(dashboardData);
        }


        public async Task<IActionResult> Solicitudes()
        {
            var resultado = VerificarRol();
            if (resultado != null) return resultado;

            var idJefeClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idJefeClaim == null || !int.TryParse(idJefeClaim.Value, out var idJefe))
            {
                TempData["Mensaje"] = "Error: No se pudo identificar al jefe.";
                TempData["TipoMensaje"] = "danger";
                return View(new List<Solicitud>());
            }


            try
            {
                // Este método trae SOLO solicitudes ACTIVAS (no Aprobadas ni Rechazadas)
                var solicitudes = await _solicitudService.ObtenerSolicitudesPorJefe(idJefe);
                return View(solicitudes ?? new List<Solicitud>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener solicitudes: {ex.Message}");
                TempData["Mensaje"] = "Error al cargar las solicitudes";
                TempData["TipoMensaje"] = "error";
                return View(new List<Solicitud>());
            }
        }

        //Carga de solicitudes Aprobadas/Rechazadas (Historial)
        public async Task<IActionResult> Historial()
        {
            var resultado = VerificarRol();
            if (resultado != null) return resultado;

            // Obtener el ID del usuario jefe (desde la sesión/claims)
            var idJefeClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idJefeClaim == null || !int.TryParse(idJefeClaim.Value, out var idJefe))
            {
                TempData["Mensaje"] = "Error: No se pudo identificar al jefe.";
                TempData["TipoMensaje"] = "danger";
                return View(new List<Solicitud>());
            }

            try
            {
                // Llama al método para obtener solicitudes FINALIZADAS.
                var historial = await _solicitudService.ObtenerHistorialPorJefe(idJefe);
                return View(historial ?? new List<Solicitud>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener historial: {ex.Message}");
                TempData["Mensaje"] = "Error al cargar el historial de solicitudes";
                TempData["TipoMensaje"] = "error";
                return View(new List<Solicitud>());
            }
        }

        public IActionResult BandejaEntrada()
        {
            var rol = HttpContext.Session.GetString("rol");
            if (rol == null)
            {

                return RedirectToAction("Index", "Login");
            }


            if (rol != "Jefe")
                return RedirectToAction("Index", "Login");
            return View();
        }
        public IActionResult Ayuda()
        {
            var rol = HttpContext.Session.GetString("rol");
            if (rol == null || rol != "Jefe")
                return RedirectToAction("Index", "Login");
            return View();
        }

        public IActionResult Configuracion()
        {
            var rol = HttpContext.Session.GetString("rol");
            if (rol == null || rol != "Jefe")
                return RedirectToAction("Index", "Login");
            return View();
        }
    }
}