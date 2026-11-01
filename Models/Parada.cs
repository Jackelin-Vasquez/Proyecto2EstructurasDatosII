//Morelia
namespace Proyecto2EstructurasDatosII.Models
{
    public class Parada
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }

        public Parada(string codigo, string nombre, double latitud, double longitud)
        {
            Codigo = codigo;
            Nombre = nombre;
            Latitud = latitud;
            Longitud = longitud;
        }

        public override string ToString()
        {
            return Codigo + " - " + Nombre +
                   " (" + Latitud + ", " + Longitud + ")";
        }
    }
}
