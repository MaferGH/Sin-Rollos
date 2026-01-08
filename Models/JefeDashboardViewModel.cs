namespace WebApp.Models.Solicitudes

{
    public class JefeDashboardViewModel
    {
        public int TotalSolicitudesPendientes { get; set; }
        public int TotalSolicitudesAprobadas { get; set; }
        public int TotalSolicitudesRechazadas { get; set; }
        public int TotalSolicitudesGeneral { get; set; }
        public List<int> TotalesPorDiaSemana { get; set; } = new List<int>();
        public List<int> TotalesRecibidasPorDia { get; set; } = new List<int>();
        public List<int> TotalesPendientesPorDia { get; set; } = new List<int>();

    }
}
