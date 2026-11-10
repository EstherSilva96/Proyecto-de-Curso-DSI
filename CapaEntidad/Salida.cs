using System;

namespace CapaEntidad
{
    public class Salida
    {
        public int idSalida { get; set; }
        public int idInventario { get; set; }
        public int idProducto { get; set; }
        public int cantidad { get; set; }
        public DateTime fecha { get; set; }
    }
}
