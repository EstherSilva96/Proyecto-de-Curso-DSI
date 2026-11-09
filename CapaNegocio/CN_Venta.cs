using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Venta
    {
        private CD_Venta objCapaDatos = new CD_Venta();
        public List<Venta> Listar()
        {
            return objCapaDatos.Listar();
        }
    }
}
