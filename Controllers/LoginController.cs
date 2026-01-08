using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using WebApp.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace WebApp.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        public LoginController(IConfiguration configuration) => _configuration = configuration;

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Index([Bind("usuario,password")] Usuario login)
        {
            string cs = _configuration.GetConnectionString("WebAppContext");

            using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();

          string sql = "SELECT Id_usuario, Nombre, usuario, rol,  Departamento,Id_empleado FROM Usuario WHERE usuario=@usuario AND password=@password AND status='Activo'";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@usuario", login.usuario);
            cmd.Parameters.AddWithValue("@password", login.password);

            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var nombre = reader["Nombre"].ToString();
                var idEmpleado = reader["Id_empleado"].ToString();
                var Departamento = reader["Departamento"].ToString();

                // ALERTA DE BIENVENIDA
                TempData["WelcomeAlert"] = $"¡Bienvenido, {nombre}!";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, reader["Id_usuario"].ToString()),
                    new Claim(ClaimTypes.Name, nombre),
                    new Claim("Usuario", reader["usuario"].ToString()),
                    new Claim("Id_empleado", idEmpleado), 
                    new Claim("Departamento", Departamento), 
                    new Claim("Rol", reader["rol"].ToString()) 
                };

                var identity = new ClaimsIdentity(claims, "CookieAuth");
                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(identity));

                HttpContext.Session.SetString("Id_empleado", idEmpleado);
                HttpContext.Session.SetString("Departamento", Departamento);
                HttpContext.Session.SetString("rol", reader["rol"].ToString()); 
                
                return RedirectToAction("RedirectByRole");
            }

            ModelState.AddModelError(string.Empty, "Usuario o contraseña inválida");
            return View(login);
        }

        [Authorize]
        public IActionResult RedirectByRole()
        {
            var rol = HttpContext.Session.GetString("rol");
            return rol switch
            {
                "Empleado" => RedirectToAction("Index", "Empleado"),
                "Jefe" => RedirectToAction("Index", "Jefe"),
                "Administrador" => RedirectToAction("Index", "Administrativo"),
                _ => RedirectToAction("Index", "Login")
            };
        }
    }
}
     