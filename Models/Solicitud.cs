// Models/Solicitudes/Solicitud.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Solicitudes
{
    public class Solicitud
    {
        public int idSolicitud { get; set; }
        public string nombreEmpleado { get; set; }
        public int numeroEmpleado { get; set; }
        public DateTime fecha { get; set; }
        public string departamentodelempleado { get; set; }
        public string departamentosolicitado { get; set; }
        public string tipoSolicitud { get; set; }
        public string estadoSolicitud { get; set; }
        public string motivoJefe { get; set; }
        public string firmaJefeBase64 { get; set; }
        public DateTime fechaCreacion { get; set; }
        public DateTime? fechaFinalizacion { get; set; }

        public string TiempoDeRespuesta
        {
            get
            {
                if (fechaFinalizacion.HasValue)
                {
                    TimeSpan duracion = fechaFinalizacion.Value - fechaCreacion;

                    return $"{duracion.Days} días, {duracion.Hours} h, {duracion.Minutes} m";
                }
                return "Pendiente";
            }
        }

    }
}


