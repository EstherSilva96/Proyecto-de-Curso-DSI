using System.Web.Mvc;

namespace ExcProy.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index() { return Pantalla("Inicio"); }
        public ActionResult Inventario() { return Pantalla("Inventario"); }
        public ActionResult Productos() { return Pantalla("Productos"); }
        public ActionResult Proveedores() { return Pantalla("Proveedores"); }
        public ActionResult Usuarios() { return Pantalla("Usuarios"); }
        public ActionResult Ventas() { return Pantalla("Ventas"); }
        public ActionResult Reportes() { return Pantalla("Reportes"); }

        private ActionResult Pantalla(string titulo)
        {
            ViewBag.Title = titulo;
            return View("Index");
        }
    }
}
