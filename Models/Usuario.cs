using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class Usuario
    {

        public int Id_usuario { get; set; }
        public int Id_empleado { get; set; }
        public string usuario { get; set; }   
        public string password { get; set; }  
        public string status { get; set; }
        public string rol { get; set; }
        public string Departamento { get; set; }
    
}

    
}
