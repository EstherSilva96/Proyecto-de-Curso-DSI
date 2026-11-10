namespace CapaEntidad
{
    public class Producto
    {
        public int idProducto { get; set; }
        public int idProveedor { get; set; }
        public string nombre { get; set; }
        public decimal precio { get; set; }
        public string categoria { get; set; }
        public bool estado { get; set; }

    }
}
