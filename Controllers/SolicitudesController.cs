using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace WebApp.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        public IActionResult NuevaSolicitud()
        {
            return View("/Views/Empleado/NuevaSolicitud.cshtml");
        }

        //cargar el partial view 
        public IActionResult FormVacaciones()
        {
            return PartialView("/Views/Empleado/Solicitudes/RH/FormVacaciones.cshtml");
        } 
        
    }
}