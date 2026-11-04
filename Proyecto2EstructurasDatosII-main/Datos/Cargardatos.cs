// Lectura de los archivos .csv de paradas y rutas.
// Los mensajes de línea ignorada se envían al callback «registro» (la interfaz los
// muestra en el registro de la pestaña «Datos y archivos»); no se escribe en consola.
using System;
using System.Globalization;
using System.IO;
using Proyecto2EstructurasDatosII.Models;

namespace Proyecto2EstructurasDatosII.Servicios
{
    public class CargadorDatos
    {
        private static void Avisar(Action<string> registro, string mensaje)
        {
            if (registro != null) registro(mensaje);
        }

        // Cantidad de líneas con datos (sin encabezado ni líneas vacías). 0 si no existe el archivo.
        public static int ContarLineasDatos(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo)) return 0;

            string[] lineas = File.ReadAllLines(rutaArchivo);
            int total = 0;
            for (int i = 1; i < lineas.Length; i++)
                if (!string.IsNullOrWhiteSpace(lineas[i])) total++;
            return total;
        }

        // Formato: codigo,nombre,latitud,longitud
        public static Parada[] LeerParadas(string rutaArchivo, Action<string> registro)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo))
            {
                Avisar(registro, "No se encontró el archivo: " + rutaArchivo);
                return new Parada[0];
            }

            string[] lineas = File.ReadAllLines(rutaArchivo);
            Parada[] buffer = new Parada[lineas.Length];
            int cantidad = 0;

            // La primera línea contiene los encabezados.
            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;

                string[] partes = lineas[i].Split(',');
                if (partes.Length != 4)
                {
                    Avisar(registro, "Línea " + (i + 1) + " ignorada: formato incorrecto (se esperaban 4 columnas).");
                    continue;
                }

                double latitud, longitud;
                bool latitudValida = double.TryParse(partes[2].Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out latitud);
                bool longitudValida = double.TryParse(partes[3].Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out longitud);

                if (!latitudValida || !longitudValida)
                {
                    Avisar(registro, "Línea " + (i + 1) + " ignorada: coordenadas inválidas.");
                    continue;
                }

                buffer[cantidad++] = new Parada(partes[0].Trim(), partes[1].Trim(), latitud, longitud);
            }

            Parada[] resultado = new Parada[cantidad];
            for (int i = 0; i < cantidad; i++) resultado[i] = buffer[i];
            return resultado;
        }

        // Formato: origen,destino,tiempo,costo,bidireccional. Devuelve cuántas rutas se leyeron.
        public static int CargarRutas(string rutaArchivo, Ruta[] rutas, Action<string> registro)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo))
            {
                Avisar(registro, "No se encontró el archivo: " + rutaArchivo);
                return 0;
            }

            int cantidad = 0;
            string[] lineas = File.ReadAllLines(rutaArchivo);

            for (int i = 1; i < lineas.Length && cantidad < rutas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;

                string[] partes = lineas[i].Split(',');
                if (partes.Length != 5)
                {
                    Avisar(registro, "Línea " + (i + 1) + " de rutas ignorada: se esperaban 5 columnas.");
                    continue;
                }

                double tiempo, costo;
                bool bidireccional;
                bool tiempoValido = double.TryParse(partes[2].Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out tiempo);
                bool costoValido = double.TryParse(partes[3].Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out costo);

                if (!tiempoValido || !costoValido || !bool.TryParse(partes[4].Trim(), out bidireccional))
                {
                    Avisar(registro, "Línea " + (i + 1) + " de rutas ignorada: datos inválidos.");
                    continue;
                }

                rutas[cantidad++] = new Ruta(partes[0].Trim(), partes[1].Trim(), tiempo, costo, bidireccional);
            }

            return cantidad;
        }
    }
}
