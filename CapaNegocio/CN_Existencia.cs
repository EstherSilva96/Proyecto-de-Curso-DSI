using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Existencia
    {
        private CD_Existencia objCapaDatos = new CD_Existencia();
        public List<Existencia> Listar()
        {
            return objCapaDatos.Listar();
        }
    }
}
