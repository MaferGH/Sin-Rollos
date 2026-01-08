using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions; // Necesario para la manipulación de Base64
using System.IO; // Necesario para la manipulación de archivos y directorios
using System.Threading.Tasks; // Necesario para async/await

namespace WebApp.Controllers.Solicitudes.RH
{
    [Authorize]
    public class FormVacacionesController : Controller
    {
        private readonly IConfiguration _configuration;

        public FormVacacionesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> GuardarVacaciones(
            IFormFile ArchivoAdjunto1, 
            string FirmaBase64Elaboro)
        {
            int idSolicitud = 0;
            var form = Request.Form;

            try
            {
                var connectionString = _configuration.GetConnectionString("WebAppContext");

                if (string.IsNullOrEmpty(connectionString))
                {
                    return Json(new { success = false, message = "No se encontró la cadena de conexión 'WebAppContext'" });
                }
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    idSolicitud = GuardarSolicitudGeneral(connection, form);
                    GuardarDatosVacaciones(connection, idSolicitud, form);
                }

                if (idSolicitud > 0)
                {
                    string rutaBase = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Archivos", "Vacaciones", idSolicitud.ToString());
                    
                    if (!Directory.Exists(rutaBase))
                    {
                        Directory.CreateDirectory(rutaBase);
                    }
                    
                    GuardarFirmaBase64(FirmaBase64Elaboro, "Firma_Elaboro.png", rutaBase);
                    
                    await GuardarArchivos(ArchivoAdjunto1, "Adjunto_1", rutaBase);
                }

                return Json(new { success = true, message = "✅ Solicitud guardada correctamente" });
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Error de MySQL: {ex.Message}");
                return Json(new { success = false, message = "Error al guardar en la base de datos." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general: {ex.Message}");
                return Json(new { success = false, message = "Error inesperado al procesar la solicitud." });
            }
        }

        private int GuardarSolicitudGeneral(MySqlConnection connection, IFormCollection form)
        {
            var query = @"
                INSERT INTO Solicitudes 
                (nombreEmpleado, numeroEmpleado, fecha, departamentodelempleado, departamentosolicitado, tipoSolicitud) 
                VALUES (@nombreEmpleado, @numeroEmpleado, @fecha, @departamentodelempleado, @departamentosolicitado, @tipoSolicitud);
                SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@nombreEmpleado", form["nombreEmpleado"].ToString());
            command.Parameters.AddWithValue("@numeroEmpleado", Convert.ToInt32(form["numeroEmpleado"]));
            command.Parameters.AddWithValue("@fecha", DateTime.Parse(form["fecha"].ToString()));
            command.Parameters.AddWithValue("@departamentodelempleado", form["departamentodelempleado"].ToString());
            command.Parameters.AddWithValue("@departamentosolicitado", form["departamentosolicitado"].ToString());
            command.Parameters.AddWithValue("@tipoSolicitud", form["tipoSolicitud"].ToString());

            return Convert.ToInt32(command.ExecuteScalar());
        }

        private void GuardarDatosVacaciones(MySqlConnection connection, int idSolicitud, IFormCollection form)
        {
            var query = @"
                INSERT INTO FormVacaciones 
                (idSolicitud, anioIngreso, diasVacaciones, fechaInicio, fechaRegreso, comentariosVacaciones) 
                VALUES (@idSolicitud, @anioIngreso, @diasVacaciones, @fechaInicio, @fechaRegreso, @comentariosVacaciones)";

            using var command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@idSolicitud", idSolicitud);
            command.Parameters.AddWithValue("@anioIngreso", Convert.ToInt32(form["anioIngreso"]));
            command.Parameters.AddWithValue("@diasVacaciones", Convert.ToInt32(form["diasVacaciones"]));
            command.Parameters.AddWithValue("@fechaInicio", DateTime.Parse(form["fechaInicio"].ToString()));
            command.Parameters.AddWithValue("@fechaRegreso", DateTime.Parse(form["fechaRegreso"].ToString()));
            command.Parameters.AddWithValue("@comentariosVacaciones", form["comentariosVacaciones"].ToString());

            command.ExecuteNonQuery();
        }


        private async Task GuardarArchivos(IFormFile archivo, string nombreBase, string rutaDirectorio)
        {
            if (archivo != null && archivo.Length > 0)
            {
                string extension = Path.GetExtension(archivo.FileName);
                string nombreArchivo = nombreBase + extension;
                var rutaCompleta = Path.Combine(rutaDirectorio, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }
            }
        }

        private void GuardarFirmaBase64(string base64, string nombreArchivo, string rutaDirectorio)
        {
            if (!string.IsNullOrEmpty(base64))
            {
                var data = Regex.Replace(base64, @"^data:image\/[a-zA-Z]+;base64,", string.Empty);
                
                try
                {
                    byte[] bytes = Convert.FromBase64String(data);
                    var rutaCompleta = Path.Combine(rutaDirectorio, nombreArchivo);
                    
                    System.IO.File.WriteAllBytes(rutaCompleta, bytes);
                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Error al convertir Base64 para {nombreArchivo}: {ex.Message}");
                }
            }
        }
    }
}