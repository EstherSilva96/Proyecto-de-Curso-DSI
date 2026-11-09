using CapaDatos;
using CapaEntidad;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class CN_Reporte
    {
        private CD_Reporte objCapaDatos = new CD_Reporte();

        public List<Reporte> Ventas(string fechainicio, string fechafin)
        {
            return objCapaDatos.Ventas(fechainicio, fechafin);
        }
        public Dashboard VerDashboard()
        {
            return objCapaDatos.VerDashboard();
        }


    }
}
