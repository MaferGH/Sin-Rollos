using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.Solicitudes;
using WebApp.Services;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace WebApp.Controllers
{
    [Authorize]
    public class AdministrativoController : Controller
    {
        private readonly ISolicitudService _solicitudService;
        private readonly IConfiguration _configuration;

        public AdministrativoController(ISolicitudService solicitudService, IConfiguration configuration)
        {
            _solicitudService = solicitudService;
            _configuration = configuration;
        }

        private IActionResult VerificarRol()
        {
            var rol = HttpContext.Session.GetString("rol");
            if (rol == null || rol != "Administrador")
            {
                return RedirectToAction("Index", "Login");
            }
            return null;
        }

        public async Task<IActionResult> Index()
        {
            var resultado = VerificarRol();
            if (resultado != null) return resultado;

            try
            {
                var model = await _solicitudService.GetAdminDashboardMetricsAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Fallo al cargar el dashboard: {ex.Message}");
                return View(new AdminDashboardViewModel());
            }
        }


        public async Task<IActionResult> Reportes(
       string departamento,
       string tipoSolicitud,
       DateTime? fechaInicio,
       DateTime? fechaFin,
       int page = 1)
        {
            var resultado = VerificarRol();
            if (resultado != null) return resultado;

            try
            {
                int pageSize = 10;

                var solicitudes = await _solicitudService.ObtenerReporteSolicitudesAsync(
                    departamento, tipoSolicitud, fechaInicio, fechaFin);

                int totalRegistros = solicitudes.Count();
                int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

                var modeloPaginado = solicitudes
                    .OrderBy(x => x.idSolicitud)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.Page = page;
                ViewBag.TotalPages = totalPaginas;

                ViewBag.FiltroDepartamento = departamento;
                ViewBag.FiltroTipo = tipoSolicitud;
                ViewBag.FiltroInicio = fechaInicio?.ToString("yyyy-MM-dd");
                ViewBag.FiltroFin = fechaFin?.ToString("yyyy-MM-dd");

                return View(modeloPaginado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Reportes: {ex.Message}");
                return View(new List<Solicitud>());
            }
        }


        public async Task<IActionResult> DescargarPDF(int id)
        {
            var resultado = VerificarRol();
            if (resultado != null) return Unauthorized();

            var solicitud = await _solicitudService.ObtenerSolicitudCompletaPorId(id);

            if (solicitud == null || (solicitud.estadoSolicitud != "Aprobada" && solicitud.estadoSolicitud != "Rechazada"))
            {
                return NotFound();
            }

            try
            {

                byte[] pdfBytes = _solicitudService.GenerarPdfReporte(solicitud);

                return File(
                    pdfBytes,
                    "application/pdf",
                    $"Reporte_Solicitud_{id}_{solicitud.tipoSolicitud}.pdf"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar el PDF con iText7: {ex.Message}");
                return StatusCode(500, "Error interno al generar el archivo PDF.");
            }
        }

        /// <summary>
        /// Muestra el Dashboard/Reporte filtrado por departamento, ahora con gráficos por día.
        /// </summary>
        public async Task<IActionResult> Historial(string departamento = "Recursos Humanos") 
        {
            var resultado = VerificarRol();
            if (resultado != null) return resultado;

            try
            {
                // 1. Llama al servicio para obtener las métricas del dashboard (JefeDashboardViewModel)
                var model = await _solicitudService.GetDashboardMetricsByDepartamentoAsync(departamento);

                // 2. Prepara los datos para la vista (Gráfico y Tarjetas)
                ViewBag.TiempoPromedioAprobacion = "2 Horas, 15 Minutos"; 
                ViewBag.SolicitudesAtrasadas = 2; 

                // DATOS ACTUALIZADOS: Calculados por día (7 puntos de datos)
                ViewBag.ChartLabels = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" }; // <--- CAMBIO AQUÍ
                ViewBag.DataSP = new[] { 15, 20, 10, 5, 25, 12, 8 }; // Pendientes (simulados)
                ViewBag.DataSR = new[] { 30, 45, 25, 20, 50, 20, 15 }; // Recibidas (simulados)

                // Valores para los filtros
                ViewBag.DepartamentoActual = departamento;
                ViewBag.FechaSeleccionada = "21 May 2025"; 
                ViewBag.Departamentos = new List<string> { "Recursos Humanos", "Finanzas", "Sistemas", "Ventas" }; 

                // 3. Retorna la vista "Historial" con el modelo correcto (JefeDashboardViewModel)
                return View("Historial", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Fallo al cargar el Dashboard Historial: {ex.Message}");
                return View("Historial", new JefeDashboardViewModel()); 
            }
        }
        
        /// <summary>
        /// Acción para el botón de "Generar Excel" del dashboard.
        /// </summary>
        public IActionResult DescargarReporteExcel(string departamento)
        {
            var resultado = VerificarRol();
            if (resultado != null) return Unauthorized();
            // Implementa aquí la lógica para generar el archivo Excel filtrado por departamento.
            return Content($"Generando reporte Excel para el departamento: {departamento}"); 
        }

        /// <summary>
        /// Acción para el botón de "Generar PDF" del dashboard.
        /// </summary>
        public IActionResult DescargarReportePDFDashboard(string departamento)
        {
            var resultado = VerificarRol();
            if (resultado != null) return Unauthorized();
            // Implementa aquí la lógica para generar el archivo PDF del dashboard filtrado.
            return Content($"Generando reporte PDF para el departamento: {departamento}");
        }


        public IActionResult BandejaEntrada()
        {
            var resultado = VerificarRol();
            if (resultado != null) return resultado;
            return View();
        }

        public IActionResult Ayuda()
        {
            var resultado = VerificarRol();
            if (resultado != null) return resultado;
            return View();
        }

        public IActionResult Configuracion()
        {
            var resultado = VerificarRol();
            if (resultado != null) return resultado;
            return View();
        }
    }
}