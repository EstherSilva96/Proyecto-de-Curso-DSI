using CapaEntidad;
using CapaNegocio;
using System.Linq;
using System.Web.Mvc;

namespace ExcProy.Controllers
{
    public class AccesoController : Controller
    {
        // GET: Acceso
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(string usuario, string clave)
        {
            // Validación temporal de prueba (sin base de datos)
            if (usuario == "admin" && clave == "123")
            {
                // Guarda valores de prueba en la sesión
                Session["usuario"] = "admin";
                Session["rol"] = "Administrador";

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Usuario o contraseña no correcta";
                return View();
            }
        }
    }
}