using System;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Solicitudes
{
	public class Empleado
	{
		public int Id_empleado { get; set; }
		public int Num_empleado { get; set; }
		public string Nombre { get; set; }
		public string Departamento { get; set; }
		public string RFC { get; set; }
		public DateTime Fecha_ingreso { get; set; }
		public int Id_jefe { get; set; }
	}
}