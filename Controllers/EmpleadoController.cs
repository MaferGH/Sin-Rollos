using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using WebApp.Models.Solicitudes;
using WebApp.Services;  


namespace WebApp.Controllers
{
	[Authorize]
	public class EmpleadoController : Controller
	{
		private readonly IConfiguration _configuration;
        private readonly ISolicitudService _solicitudService;  

		  public EmpleadoController(IConfiguration configuration, ISolicitudService solicitudService)
        {
            _configuration = configuration;
            _solicitudService = solicitudService;
        }


		private IActionResult VerificarRol()
		{
			var rol = HttpContext.Session.GetString("rol");
			if (rol == null || rol != "Empleado")
				return RedirectToAction("Index", "Login");
			return null;
		}

		public async Task<IActionResult> MisSolicitudes()
		{
			var resultado = VerificarRol();
			if (resultado != null) return resultado;

			var idEmpleado = User.FindFirst("Id_empleado")?.Value;

			if (string.IsNullOrEmpty(idEmpleado))
			{
				TempData["Error"] = "No se encontró el ID del empleado";
				return RedirectToAction("Index");
			}

			
			var solicitudes = await ObtenerSolicitudesPorEmpleado(idEmpleado);

			ViewBag.IdEmpleado = idEmpleado;
			ViewBag.Departamento = User.FindFirst("Departamento")?.Value;

			return View(solicitudes);
		}

		private async Task<List<Solicitud>> ObtenerSolicitudesPorEmpleado(string idEmpleado)
		{
			var solicitudes = new List<Solicitud>();
			string cs = _configuration.GetConnectionString("WebAppContext");

			try
			{
				using var conn = new MySqlConnection(cs);
				await conn.OpenAsync();

				string sql = @"
                    SELECT 
                        idSolicitud, nombreEmpleado, numeroEmpleado, fecha, 
                        departamentodelempleado, departamentosolicitado, tipoSolicitud, estadoSolicitud, fechaCreacion
                    FROM Solicitudes 
                    WHERE numeroEmpleado = @idEmpleado 
                    ORDER BY fecha DESC";

				using var cmd = new MySqlCommand(sql, conn);
				cmd.Parameters.AddWithValue("@idEmpleado", idEmpleado);

				using var reader = await cmd.ExecuteReaderAsync();
				while (await reader.ReadAsync())
				{
					var solicitud = new Solicitud
					{
						idSolicitud = reader["idSolicitud"] != DBNull.Value ? Convert.ToInt32(reader["idSolicitud"]) : 0,
						nombreEmpleado = reader["nombreEmpleado"]?.ToString(),
						numeroEmpleado = reader["numeroEmpleado"] != DBNull.Value ? Convert.ToInt32(reader["numeroEmpleado"]) : 0,
						fecha = reader["fecha"] != DBNull.Value ? Convert.ToDateTime(reader["fecha"]) : DateTime.MinValue,
						departamentodelempleado = reader["departamentodelempleado"]?.ToString(),
						departamentosolicitado = reader["departamentosolicitado"]?.ToString(),
						tipoSolicitud = reader["tipoSolicitud"]?.ToString(),
						estadoSolicitud = reader["estadoSolicitud"]?.ToString(),
						fechaCreacion = reader["fechaCreacion"] != DBNull.Value ? Convert.ToDateTime(reader["fechaCreacion"]) : DateTime.MinValue
					};
					solicitudes.Add(solicitud);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error al obtener solicitudes: {ex.Message}");
			}

			return solicitudes;
		}

		[HttpPost]
        public async Task<IActionResult> EnviarSolicitud(int idSolicitud)
        {
            var resultado = VerificarRol();
            if (resultado != null) return resultado;

            try
            {
                var exito = await _solicitudService.EnviarSolicitudAJefe(idSolicitud);
                
                if (exito)
                {
                    TempData["Mensaje"] = "Solicitud enviada al jefe departamental exitosamente";
                    TempData["TipoMensaje"] = "success";
                }
                else
                {
                    TempData["Mensaje"] = "Error: No se encontró un jefe asignado para su departamento";
                    TempData["TipoMensaje"] = "error";
                }
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = "Error al procesar la solicitud: " + ex.Message;
                TempData["TipoMensaje"] = "error";
            }

            return RedirectToAction("MisSolicitudes");
        }
    

		public IActionResult Index()
		{
			var rol = HttpContext.Session.GetString("rol");
			if (rol == null)
			{

				return RedirectToAction("Index", "Login");
			}


			if (rol != "Empleado")
				return RedirectToAction("Index", "Login");

			return View();

		}

		public IActionResult NuevaSolicitud()
		{
			var rol = HttpContext.Session.GetString("rol");
			if (rol == null)
			{

				return RedirectToAction("Index", "Login");
			}


			if (rol != "Empleado")
				return RedirectToAction("Index", "Login");

			return View();
		}

		public IActionResult BandejaEntrada()
		{
			var rol = HttpContext.Session.GetString("rol");
			if (rol == null)
			{

				return RedirectToAction("Index", "Login");
			}


			if (rol != "Empleado")
				return RedirectToAction("Index", "Login");

			return View();
		}

		public IActionResult Ayuda()
		{
			var rol = HttpContext.Session.GetString("rol");
			if (rol == null)
			{

				return RedirectToAction("Index", "Login");
			}


			if (rol != "Empleado")
				return RedirectToAction("Index", "Login");

			return View();
		}

		public IActionResult Configuracion()
		{
			var rol = HttpContext.Session.GetString("rol");
			if (rol == null)
			{

				return RedirectToAction("Index", "Login");
			}


			if (rol != "Empleado")
				return RedirectToAction("Index", "Login");

			return View();
		}
	}
}










      

