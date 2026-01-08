using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class FormVacacion
    {
        public int idVacaciones { get; set; }
        public int idSolicitud { get; set; }
        public int anioIngreso { get; set; }
        public int diasVacaciones { get; set; }
        public DateTime fechaInicio { get; set; }
        public DateTime fechaRegreso { get; set; }
        public string comentariosVacaciones { get; set; }

    }
}