using System;
using System.Configuration;

namespace CapaDatos
{
    public class Conexion
    {
        public static string cn = ObtenerCadena();

        private static string ObtenerCadena()
        {
            string cadena = Environment.GetEnvironmentVariable(
                "DSI_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(cadena))
                return cadena;

            var configuracion =
                ConfigurationManager.ConnectionStrings["cadena"];
            if (configuracion == null || string.IsNullOrWhiteSpace(
                configuracion.ConnectionString))
                throw new ConfigurationErrorsException(
                    "No se configuro la conexion cadena.");

            return configuracion.ConnectionString;
        }
    }
}