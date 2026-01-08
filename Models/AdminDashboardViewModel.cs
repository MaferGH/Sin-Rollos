namespace WebApp.Models.Solicitudes
{
    public class DepartamentoTiempoPromedio
    {
        public string NombreDepartamento { get; set; }
        public double TiempoPromedioHoras { get; set; }
        public int TotalCompletadas { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalSolicitudesPendientes { get; set; }
        public int TotalSolicitudesAprobadas { get; set; }
        public int TotalSolicitudesRechazadas { get; set; }
        public int TotalSolicitudesGeneral { get; set; }

        public List<DepartamentoTiempoPromedio> TiemposPorDepartamento { get; set; } = new List<DepartamentoTiempoPromedio>();

        public Dictionary<string, int> TotalesPorTipoSolicitud { get; set; } = new Dictionary<string, int>();

        public double TasaAprobacionGlobal { get; set; }

        public List<int> TotalesPorDiaSemana { get; set; } = new List<int>();
        public Dictionary<string, int> TotalesPorEstadoSolicitud { get; set; } = new Dictionary<string, int>();
   
    }
}