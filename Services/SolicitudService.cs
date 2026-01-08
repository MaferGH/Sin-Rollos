using MySql.Data.MySqlClient;
using WebApp.Models.Solicitudes;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using QuestPDF.Drawing;
using System.Linq;
using System;
using MySql.Data.MySqlClient;
using WebApp.Models.Solicitudes;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Document = QuestPDF.Fluent.Document;


namespace WebApp.Services
{
    public interface ISolicitudService
    {

        Task<JefeDashboardViewModel> GetDashboardMetricsByDepartamentoAsync(string departamento);

        Task<bool> EnviarSolicitudAJefe(int idSolicitud);

        Task<bool> AprobarSolicitud(int idSolicitud, string motivo, string firmaBase64);
        Task<bool> RechazarSolicitud(int idSolicitud, string motivo, string firmaBase64);

        Task<List<Solicitud>> ObtenerSolicitudesPorJefe(int idJefe);

        Task<List<Solicitud>> ObtenerHistorialPorJefe(int idJefe);
        Task<List<Solicitud>> ObtenerReporteSolicitudesAsync(string departamento, string tipoSolicitud, DateTime? fechaInicio, DateTime? fechaFin);
        Task<Solicitud> ObtenerSolicitudCompletaPorId(int idSolicitud);
        byte[] GenerarPdfReporte(Solicitud solicitud);

        Task<AdminDashboardViewModel> GetAdminDashboardMetricsAsync();
    }

    public class SolicitudService : ISolicitudService
    {
        private readonly IConfiguration _configuration;

        public SolicitudService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> EnviarSolicitudAJefe(int idSolicitud)
        {
            string cs = _configuration.GetConnectionString("WebAppContext");
            try
            {
                using var conn = new MySqlConnection(cs);
                await conn.OpenAsync();

                string sqlSolicitud = @"
                    SELECT departamentodelempleado 
                    FROM Solicitudes 
                    WHERE idSolicitud = @idSolicitud";
                return true;
            }
            catch (Exception) { return false; }
        }


        public async Task<bool> AprobarSolicitud(int idSolicitud, string motivo, string firmaBase64)
        {
            string cs = _configuration.GetConnectionString("WebAppContext");
            try
            {
                using var conn = new MySqlConnection(cs);
                await conn.OpenAsync();
                string sql = @"
            UPDATE Solicitudes 
            SET 
                estadoSolicitud = 'Aprobada',
                motivoJefe = @motivo, 
                firmaJefeBase64 = @firma,
                fechaFinalizacion = NOW()
            WHERE idSolicitud = @idSolicitud";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idSolicitud", idSolicitud);
                cmd.Parameters.AddWithValue("@motivo", motivo);
                cmd.Parameters.AddWithValue("@firma", firmaBase64);

                var result = await cmd.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR SolicitudService - Aprobar] Falló la actualización de BD: {ex.Message}");
                return false;
            }
        }


        public async Task<List<Solicitud>> ObtenerReporteSolicitudesAsync(string departamento, string tipoSolicitud, DateTime? fechaInicio, DateTime? fechaFin)
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
                departamentodelempleado, departamentosolicitado, tipoSolicitud, estadoSolicitud, 
                motivoJefe, firmaJefeBase64, fechaCreacion, fechaFinalizacion
            FROM Solicitudes 
            WHERE estadoSolicitud IN ('Aprobada', 'Rechazada')
        ";

                var conditions = new List<string>();
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(departamento))
                {
                    conditions.Add("departamentosolicitado = @Departamento");
                    parameters.Add(new MySqlParameter("@Departamento", departamento));
                }

                if (!string.IsNullOrEmpty(tipoSolicitud))
                {
                    conditions.Add("tipoSolicitud = @TipoSolicitud");
                    parameters.Add(new MySqlParameter("@TipoSolicitud", tipoSolicitud));
                }

                if (fechaInicio.HasValue)
                {
                    conditions.Add("fechaCreacion >= @FechaInicio");
                    parameters.Add(new MySqlParameter("@FechaInicio", fechaInicio.Value.Date));
                }

                if (fechaFin.HasValue)
                {
                    // Filtra hasta el final del día (usando < al día siguiente)
                    conditions.Add("fechaCreacion < @FechaFin");
                    parameters.Add(new MySqlParameter("@FechaFin", fechaFin.Value.Date.AddDays(1)));
                }

                if (conditions.Any())
                {
                    sql += " AND " + string.Join(" AND ", conditions);
                }

                sql += " ORDER BY fechaCreacion DESC";


                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var solicitud = new Solicitud
                    {
                        idSolicitud = reader["idSolicitud"] != DBNull.Value ? Convert.ToInt32(reader["idSolicitud"]) : 0,
                        nombreEmpleado = reader["nombreEmpleado"]?.ToString(),
                        numeroEmpleado = reader["numeroEmpleado"] != DBNull.Value ? Convert.ToInt32(reader["numeroEmpleado"]) : 0,
                        departamentodelempleado = reader["departamentodelempleado"]?.ToString(),
                        departamentosolicitado = reader["departamentosolicitado"]?.ToString(),
                        tipoSolicitud = reader["tipoSolicitud"]?.ToString(),
                        estadoSolicitud = reader["estadoSolicitud"]?.ToString(),
                        motivoJefe = reader["motivoJefe"]?.ToString(),
                        firmaJefeBase64 = reader["firmaJefeBase64"]?.ToString(),
                        fechaCreacion = reader["fechaCreacion"] != DBNull.Value ? Convert.ToDateTime(reader["fechaCreacion"]) : DateTime.MinValue,
                        // Las fechas finales pueden ser nulas
                        fechaFinalizacion = reader["fechaFinalizacion"] != DBNull.Value ? (DateTime?)reader["fechaFinalizacion"] : null
                    };
                    solicitudes.Add(solicitud);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el reporte de solicitudes: {ex.Message}");
            }

            return solicitudes;
        }


        public async Task<Solicitud> ObtenerSolicitudCompletaPorId(int idSolicitud)
        {
            string cs = _configuration.GetConnectionString("WebAppContext");
            Solicitud solicitud = null;

            string sql = @"
        SELECT 
            idSolicitud, nombreEmpleado, numeroEmpleado, fecha, 
            departamentodelempleado, departamentosolicitado, tipoSolicitud, estadoSolicitud, 
            motivoJefe, firmaJefeBase64, fechaCreacion, fechaFinalizacion
        FROM Solicitudes 
        WHERE idSolicitud = @idSolicitud";

            try
            {
                using var conn = new MySqlConnection(cs);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idSolicitud", idSolicitud);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    solicitud = new Solicitud
                    {
                        idSolicitud = reader["idSolicitud"] != DBNull.Value ? Convert.ToInt32(reader["idSolicitud"]) : 0,
                        nombreEmpleado = reader["nombreEmpleado"]?.ToString(),
                        numeroEmpleado = reader["numeroEmpleado"] != DBNull.Value ? Convert.ToInt32(reader["numeroEmpleado"]) : 0,
                        fecha = reader["fecha"] != DBNull.Value ? Convert.ToDateTime(reader["fecha"]) : DateTime.MinValue,
                        departamentodelempleado = reader["departamentodelempleado"]?.ToString(),
                        departamentosolicitado = reader["departamentosolicitado"]?.ToString(),
                        tipoSolicitud = reader["tipoSolicitud"]?.ToString(),
                        estadoSolicitud = reader["estadoSolicitud"]?.ToString(),
                        motivoJefe = reader["motivoJefe"]?.ToString(),
                        firmaJefeBase64 = reader["firmaJefeBase64"]?.ToString(),
                        fechaCreacion = reader["fechaCreacion"] != DBNull.Value ? Convert.ToDateTime(reader["fechaCreacion"]) : DateTime.MinValue,
                        fechaFinalizacion = reader["fechaFinalizacion"] != DBNull.Value ? (DateTime?)reader["fechaFinalizacion"] : null
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener solicitud por ID: {ex.Message}");
            }
            return solicitud;
        }

        public byte[] GenerarPdfReporte(Solicitud solicitud)
        {
            Action<IContainer> firmaRenderer = container =>
            {
                if (string.IsNullOrEmpty(solicitud.firmaJefeBase64))
                {
                    container.Text("No hay firma registrada.");
                    return;
                }

                try
                {
                    string base64Data = solicitud.firmaJefeBase64.Split(',').Last();
                    byte[] imageBytes = Convert.FromBase64String(base64Data);

                    container
                        .Height(100)
                        .Width(250)
                        .Image(imageBytes, ImageScaling.FitArea);
                }
                catch (Exception)
                {
                    container.Text("Error: No se pudo renderizar la imagen de la firma (Base64 inválido o corrupto).")
                        .FontColor(Colors.Red.Medium);
                }
            };


            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Text($"REPORTE ADMINISTRATIVO SOLICITUD #{solicitud.idSolicitud}")
                            .Bold().FontSize(18).FontColor(Colors.Blue.Darken2).AlignCenter();
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(15);

                        column.Item().Text("Detalles de la Solicitud").FontSize(14).SemiBold().Underline();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(3);
                            });

                            table.Cell().Text("Tipo:").SemiBold();
                            table.Cell().Text(solicitud.tipoSolicitud);
                            table.Cell().Text("Empleado:").SemiBold();
                            table.Cell().Text($"{solicitud.nombreEmpleado} (No. {solicitud.numeroEmpleado})");

                            table.Cell().Text("Depto. Solicitante:").SemiBold();
                            table.Cell().Text(solicitud.departamentodelempleado);
                            table.Cell().Text("Depto. Destino:").SemiBold();
                            table.Cell().Text(solicitud.departamentosolicitado);

                            table.Cell().Text("Creación:").SemiBold();
                            table.Cell().Text(solicitud.fechaCreacion.ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Text("Finalización:").SemiBold();
                            table.Cell().Text(solicitud.fechaFinalizacion?.ToString("dd/MM/yyyy HH:mm") ?? "N/A");

                            table.Cell().Text("Tiempo Respuesta:").SemiBold();
                            table.Cell().Text(solicitud.TiempoDeRespuesta);
                        });


                        column.Item().PaddingTop(15).Text("Resolución Final").FontSize(14).SemiBold().Underline();

                        var estadoColor = solicitud.estadoSolicitud == "Aprobada" ? Colors.Green.Medium : Colors.Red.Medium;

                        column.Item().Text(text =>
                        {
                            text.Span("Estado: ").SemiBold();
                            text.Span(solicitud.estadoSolicitud).FontColor(estadoColor).SemiBold().FontSize(12);
                        });

                        column.Item().Text(text =>
                        {
                            text.Span("Motivo del Jefe: ").SemiBold();
                            text.Span(solicitud.motivoJefe).Italic();
                        });

                        column.Item().PaddingTop(20).Text("Firma de Autorización del Jefe").FontSize(14).SemiBold().Underline();
                        column.Item().Element(firmaRenderer);
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });

                });
            })
            .GeneratePdf();
        }


        public async Task<AdminDashboardViewModel> GetAdminDashboardMetricsAsync()
        {
            var metrics = new AdminDashboardViewModel();
            string cs = _configuration.GetConnectionString("WebAppContext");

            try
            {
                using var conn = new MySqlConnection(cs);
                await conn.OpenAsync();

                string sqlTotales = @"
            SELECT 
                SUM(CASE WHEN estadoSolicitud IS NULL OR estadoSolicitud NOT IN ('Aprobada', 'Rechazada') THEN 1 ELSE 0 END) AS Pendientes,
                SUM(CASE WHEN estadoSolicitud = 'Aprobada' THEN 1 ELSE 0 END) AS Aprobadas,
                SUM(CASE WHEN estadoSolicitud = 'Rechazada' THEN 1 ELSE 0 END) AS Rechazadas,
                COUNT(*) AS Total
            FROM Solicitudes";

                using (var cmdTotales = new MySqlCommand(sqlTotales, conn))
                using (var readerTotales = await cmdTotales.ExecuteReaderAsync())
                {
                    if (await readerTotales.ReadAsync())
                    {
                        metrics.TotalSolicitudesPendientes = Convert.ToInt32(readerTotales["Pendientes"]);
                        metrics.TotalSolicitudesAprobadas = Convert.ToInt32(readerTotales["Aprobadas"]);
                        metrics.TotalSolicitudesRechazadas = Convert.ToInt32(readerTotales["Rechazadas"]);
                        metrics.TotalSolicitudesGeneral = Convert.ToInt32(readerTotales["Total"]);

                        if (metrics.TotalSolicitudesAprobadas + metrics.TotalSolicitudesRechazadas > 0)
                        {
                            metrics.TasaAprobacionGlobal = (double)metrics.TotalSolicitudesAprobadas / (metrics.TotalSolicitudesAprobadas + metrics.TotalSolicitudesRechazadas) * 100;
                        }

                        metrics.TotalesPorEstadoSolicitud.Add("Aprobada", metrics.TotalSolicitudesAprobadas);
                        metrics.TotalesPorEstadoSolicitud.Add("Rechazada", metrics.TotalSolicitudesRechazadas);
                        metrics.TotalesPorEstadoSolicitud.Add("Pendiente", metrics.TotalSolicitudesPendientes);
                    }
                }

                string sqlTiempos = @"
            SELECT
                departamentodelempleado AS Nombre,
                COUNT(*) AS Total,
                AVG(TIMESTAMPDIFF(SECOND, fechaCreacion, fechaFinalizacion)) / 3600 AS TiempoPromedioHoras 
            FROM Solicitudes
            WHERE estadoSolicitud IN ('Aprobada', 'Rechazada')
            GROUP BY departamentodelempleado
            HAVING COUNT(*) > 0";

                using (var cmdTiempos = new MySqlCommand(sqlTiempos, conn))
                using (var readerTiempos = await cmdTiempos.ExecuteReaderAsync())
                {
                    while (await readerTiempos.ReadAsync())
                    {
                        metrics.TiemposPorDepartamento.Add(new DepartamentoTiempoPromedio
                        {
                            NombreDepartamento = readerTiempos["Nombre"]?.ToString(),
                            TiempoPromedioHoras = readerTiempos["TiempoPromedioHoras"] != DBNull.Value ? Convert.ToDouble(readerTiempos["TiempoPromedioHoras"]) : 0,
                            TotalCompletadas = Convert.ToInt32(readerTiempos["Total"])
                        });
                    }
                }

                string sqlTipos = @"
            SELECT tipoSolicitud, COUNT(*) AS Total
            FROM Solicitudes
            GROUP BY tipoSolicitud";

                using (var cmdTipos = new MySqlCommand(sqlTipos, conn))
                using (var readerTipos = await cmdTipos.ExecuteReaderAsync())
                {
                    while (await readerTipos.ReadAsync())
                    {
                        metrics.TotalesPorTipoSolicitud.Add(
                            readerTipos["tipoSolicitud"]?.ToString(),
                            Convert.ToInt32(readerTipos["Total"])
                        );
                    }
                }

                string sqlSemana = @"
            SELECT 
                DATE_FORMAT(fechaCreacion, '%w') AS DiaSemana, 
                COUNT(*) AS Total
            FROM Solicitudes 
            WHERE 
                fechaCreacion >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)
            GROUP BY DiaSemana
            ORDER BY DiaSemana ASC";

                var totalesPorDia = new int[7];
                using (var cmdSemana = new MySqlCommand(sqlSemana, conn))
                using (var readerSemana = await cmdSemana.ExecuteReaderAsync())
                {
                    while (await readerSemana.ReadAsync())
                    {
                        int diaSemana = Convert.ToInt32(readerSemana["DiaSemana"]);
                        int total = Convert.ToInt32(readerSemana["Total"]);
                        totalesPorDia[diaSemana] = total;
                    }
                }
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[1]);
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[2]);
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[3]);
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[4]);
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[5]);
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[6]);
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[0]);

                return metrics;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR SolicitudService - GetAdminDashboardMetricsAsync] Falló la consulta: {ex.Message}");
                return new AdminDashboardViewModel();
            }
        }

        public async Task<JefeDashboardViewModel> GetDashboardMetricsByDepartamentoAsync(string departamento)
        {
            var metrics = new JefeDashboardViewModel();
            string cs = _configuration.GetConnectionString("WebAppContext");

            try
            {
                using var conn = new MySqlConnection(cs);
                await conn.OpenAsync();

                var departamentoJefe = departamento;

                // Consulta de Solicitudes Pendientes 
                string sqlPendientes = @"
                    SELECT COUNT(*) FROM Solicitudes 
                    WHERE departamentodelempleado = @Departamento 
                    AND (estadoSolicitud IS NULL OR estadoSolicitud = '' OR estadoSolicitud NOT IN ('Aprobada', 'Rechazada'))";
                using var cmdPendientes = new MySqlCommand(sqlPendientes, conn);
                cmdPendientes.Parameters.AddWithValue("@Departamento", departamentoJefe);
                metrics.TotalSolicitudesPendientes = Convert.ToInt32(await cmdPendientes.ExecuteScalarAsync());

                // Consulta de Solicitudes Aprobadas
                string sqlAprobadas = "SELECT COUNT(*) FROM Solicitudes WHERE departamentodelempleado = @Departamento AND estadoSolicitud = 'Aprobada'";
                using var cmdAprobadas = new MySqlCommand(sqlAprobadas, conn);
                cmdAprobadas.Parameters.AddWithValue("@Departamento", departamentoJefe);
                metrics.TotalSolicitudesAprobadas = Convert.ToInt32(await cmdAprobadas.ExecuteScalarAsync());

                // Consulta de Solicitudes Rechazadas
                string sqlRechazadas = "SELECT COUNT(*) FROM Solicitudes WHERE departamentodelempleado = @Departamento AND estadoSolicitud = 'Rechazada'";
                using var cmdRechazadas = new MySqlCommand(sqlRechazadas, conn);
                cmdRechazadas.Parameters.AddWithValue("@Departamento", departamentoJefe);
                metrics.TotalSolicitudesRechazadas = Convert.ToInt32(await cmdRechazadas.ExecuteScalarAsync());

                // Total General
                metrics.TotalSolicitudesGeneral = metrics.TotalSolicitudesPendientes + metrics.TotalSolicitudesAprobadas + metrics.TotalSolicitudesRechazadas;

                string sqlSemana = @"
            SELECT 
                DATE_FORMAT(fechaCreacion, '%w') AS DiaSemana, 
                COUNT(*) AS Total
            FROM Solicitudes 
            WHERE 
                departamentodelempleado = @Departamento
                AND fechaCreacion >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)
            GROUP BY DiaSemana
            ORDER BY DiaSemana ASC";

                var totalesPorDia = new int[7];

                using (var cmdSemana = new MySqlCommand(sqlSemana, conn))
                {
                    cmdSemana.Parameters.AddWithValue("@Departamento", departamentoJefe);

                    using var readerSemana = await cmdSemana.ExecuteReaderAsync();
                    while (await readerSemana.ReadAsync())
                    {
                        int diaSemana = Convert.ToInt32(readerSemana["DiaSemana"]);
                        int total = Convert.ToInt32(readerSemana["Total"]);
                        totalesPorDia[diaSemana] = total;
                    }
                }

                metrics.TotalesPorDiaSemana.Add(totalesPorDia[1]); // Lunes
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[2]); // Martes
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[3]); // Miércoles
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[4]); // Jueves
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[5]); // Viernes
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[6]); // Sábado
                metrics.TotalesPorDiaSemana.Add(totalesPorDia[0]); // Domingo

                return metrics;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR SolicitudService - GetDashboardMetricsByDepartamentoAsync] Falló la consulta: {ex.Message}");
                return new JefeDashboardViewModel();
            }

        }

        public async Task<bool> RechazarSolicitud(int idSolicitud, string motivo, string firmaBase64)
        {
            string cs = _configuration.GetConnectionString("WebAppContext");
            try
            {
                using var conn = new MySqlConnection(cs);
                await conn.OpenAsync();
                string sql = @"
            UPDATE Solicitudes 
            SET 
                estadoSolicitud = 'Rechazada',
                motivoJefe = @motivo, 
                firmaJefeBase64 = @firma,
                fechaFinalizacion = NOW()

            WHERE idSolicitud = @idSolicitud";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idSolicitud", idSolicitud);
                cmd.Parameters.AddWithValue("@motivo", motivo);
                cmd.Parameters.AddWithValue("@firma", firmaBase64);

                var result = await cmd.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR SolicitudService - Rechazar] Falló la actualización de BD: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Solicitud>> ObtenerSolicitudesPorJefe(int idJefe)
        {
            var solicitudes = new List<Solicitud>();
            string cs = _configuration.GetConnectionString("WebAppContext");

            try
            {
                using var conn = new MySqlConnection(cs);
                await conn.OpenAsync();

                // Obtener el DEPARTAMENTO del jefe actual
                string sqlDepartamentoJefe = @"
                    SELECT Departamento 
                    FROM Usuario 
                    WHERE Id_usuario = @IdJefe 
                    AND rol = 'Jefe'";

                using var cmdDepto = new MySqlCommand(sqlDepartamentoJefe, conn);
                cmdDepto.Parameters.AddWithValue("@IdJefe", idJefe);

                var departamentoJefe = await cmdDepto.ExecuteScalarAsync() as string;

                if (string.IsNullOrEmpty(departamentoJefe))
                {
                    return solicitudes;
                }

                // Obtener solicitudes ACTIVAS (no Aprobadas ni Rechazadas)
                string sql = @"
    SELECT 
        idSolicitud, nombreEmpleado, numeroEmpleado, fecha, 
        departamentodelempleado, departamentosolicitado, tipoSolicitud, estadoSolicitud, fechaCreacion
    FROM Solicitudes 
    WHERE departamentodelempleado = @Departamento 

    AND (
        estadoSolicitud IS NULL 
        OR estadoSolicitud = '' 
        OR estadoSolicitud NOT IN ('Aprobada', 'Rechazada')
    )
    

    ORDER BY fechaCreacion ASC";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Departamento", departamentoJefe);

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

        // MÉTODO Para obtener el Historial (solo solicitudes finalizadas)
        public async Task<List<Solicitud>> ObtenerHistorialPorJefe(int idJefe)
        {
            var solicitudes = new List<Solicitud>();
            string cs = _configuration.GetConnectionString("WebAppContext");

            try
            {
                using var conn = new MySqlConnection(cs);
                await conn.OpenAsync();

                // Obtener el DEPARTAMENTO del jefe actual (igual que el otro método)
                string sqlDepartamentoJefe = @"
                    SELECT Departamento 
                    FROM Usuario 
                    WHERE Id_usuario = @IdJefe 
                    AND rol = 'Jefe'";

                using var cmdDepto = new MySqlCommand(sqlDepartamentoJefe, conn);
                cmdDepto.Parameters.AddWithValue("@IdJefe", idJefe);

                var departamentoJefe = await cmdDepto.ExecuteScalarAsync() as string;

                if (string.IsNullOrEmpty(departamentoJefe))
                {
                    return solicitudes;
                }

                // Obtener solicitudes FINALIZADAS (Aprobadas o Rechazadas)
                string sql = @"
                    SELECT 
                        idSolicitud, nombreEmpleado, numeroEmpleado, fecha, 
                        departamentodelempleado, departamentosolicitado, tipoSolicitud, estadoSolicitud, fechaCreacion
                    FROM Solicitudes 
                    WHERE departamentodelempleado = @Departamento 

                    AND estadoSolicitud IN ('Aprobada', 'Rechazada')

                    ORDER BY fechaCreacion DESC";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Departamento", departamentoJefe);

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
                Console.WriteLine($"Error al obtener historial: {ex.Message}");
            }

            return solicitudes;
        }
    }
}