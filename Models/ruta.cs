//Jackelin Vásquez

namespace Proyecto2EstructurasDatosII.Models
{
    public class Ruta
    {
        public string CodigoOrigen { get; set; }
        public string CodigoDestino { get; set; }
        public double Peso { get; set; } // Tiempo en minutos/costo
        public bool EsUnSentido { get; set; } // True si solo opera en una dirección

        public Ruta(string codigoOrigen, string codigoDestino, double peso, bool esUnSentido)
        {
            CodigoOrigen = codigoOrigen;
            CodigoDestino = codigoDestino;
            Peso = peso;
            EsUnSentido = esUnSentido;
        }
    }
}