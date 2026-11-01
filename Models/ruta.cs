//Jackelin Vásquez

namespace Proyecto2EstructurasDatosII.Models
{
    public class Ruta
    {
        public string Origen { get; set; }
        public string Destino { get; set; }
        public double Tiempo { get; set; }
        public double Costo { get; set; }
        public bool EsBidireccional { get; set; }

        public Ruta(string origen, string destino, double tiempo,
                    double costo, bool esBidireccional)
        {
            Origen = origen;
            Destino = destino;
            Tiempo = tiempo;
            Costo = costo;
            EsBidireccional = esBidireccional;
        }

        public override string ToString()
        {
            string sentido = EsBidireccional ? "Bidireccional" : "Un solo sentido";
            return Origen + " -> " + Destino +
                   " | Tiempo: " + Tiempo +
                   " | Costo: Q" + Costo +
                   " | " + sentido;
        }
    }
}
