using System.Globalization;
using Proyecto2EstructurasDatosII.Estructuras;
using Proyecto2EstructurasDatosII.Models;

namespace Proyecto2EstructurasDatosII.Servicios
{
    public class CargadorDatos
    {
        public static int CargarParadas(string rutaArchivo, TablaHash tabla)
        {
            if (!File.Exists(rutaArchivo))
            {
                Console.WriteLine("No se encontró el archivo: " + rutaArchivo);
                return 0;
            }

            int cargadas = 0;
            string[] lineas = File.ReadAllLines(rutaArchivo);

            // La primera línea contiene los encabezados.
            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i]))
                    continue;

                string[] partes = lineas[i].Split(',');

                if (partes.Length != 4)
                {
                    Console.WriteLine("Línea " + (i + 1) + " ignorada: formato incorrecto.");
                    continue;
                }

                double latitud, longitud;

                bool latitudValida = double.TryParse(partes[2].Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out latitud);
                bool longitudValida = double.TryParse(partes[3].Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out longitud);

                if (!latitudValida || !longitudValida)
                {
                    Console.WriteLine("Línea " + (i + 1) + " ignorada: coordenadas inválidas.");
                    continue;
                }

                Parada parada = new Parada(
                    partes[0].Trim(),
                    partes[1].Trim(),
                    latitud,
                    longitud
                );

                if (tabla.Insertar(parada))
                    cargadas++;
                else
                    Console.WriteLine("Código repetido o inválido: " + partes[0]);
            }

            return cargadas;
        }

        public static int CargarRutas(string rutaArchivo, Ruta[] rutas)
        {
            if (!File.Exists(rutaArchivo))
            {
                Console.WriteLine("No se encontró el archivo: " + rutaArchivo);
                return 0;
            }

            int cantidad = 0;
            string[] lineas = File.ReadAllLines(rutaArchivo);

            for (int i = 1; i < lineas.Length && cantidad < rutas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i]))
                    continue;

                string[] partes = lineas[i].Split(',');

                if (partes.Length != 5)
                {
                    Console.WriteLine("Línea " + (i + 1) + " de rutas ignorada.");
                    continue;
                }

                double tiempo, costo;
                bool tiempoValido = double.TryParse(partes[2].Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out tiempo);
                bool costoValido = double.TryParse(partes[3].Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out costo);
                bool bidireccional;

                if (!tiempoValido || !costoValido ||
                    !bool.TryParse(partes[4].Trim(), out bidireccional))
                {
                    Console.WriteLine("Línea " + (i + 1) + " de rutas contiene datos inválidos.");
                    continue;
                }

                rutas[cantidad] = new Ruta(
                    partes[0].Trim(),
                    partes[1].Trim(),
                    tiempo,
                    costo,
                    bidireccional
                );

                cantidad++;
            }

            return cantidad;
        }
    }
}
