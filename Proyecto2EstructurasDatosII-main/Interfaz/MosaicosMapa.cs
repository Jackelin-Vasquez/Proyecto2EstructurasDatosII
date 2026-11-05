// Proveedor de mosaicos (tiles) del mapa real de OpenStreetMap.
// - Los mosaicos se descargan en segundo plano para no congelar la interfaz.
// - Se guardan en disco (carpeta local del usuario) para no descargarlos otra vez.
// - Si no hay Internet, simplemente no se dibujan y el mapa sigue mostrando las paradas.
// Política de uso de OSM: se envía un User-Agent que identifica la aplicación y se limita
// la cantidad de descargas simultáneas.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    public static class MosaicosMapa
    {
        // Plantilla de la URL de mosaicos. Puede cambiarse por otro proveedor compatible.
        public static string UrlMosaicos = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
        public const string Atribucion = "© Colaboradores de OpenStreetMap";

        // Se dispara (desde otro hilo) cada vez que llega un mosaico o falla una descarga.
        public static event Action Actualizado;

        // Último error de descarga (útil para diagnosticar problemas de red).
        public static string UltimoError { get; private set; }

        private static readonly object cerrojo = new object();
        private static readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();
        private static readonly HashSet<string> pendientes = new HashSet<string>();
        private static readonly Dictionary<string, DateTime> fallidos = new Dictionary<string, DateTime>();

        // Cola de trabajo atendida por hilos propios (no por el ThreadPool, para evitar bloqueos:
        // las descargas HTTP necesitan hilos libres del ThreadPool para terminar).
        private const int Trabajadores = 4;
        private const int MaxCola = 150;
        private static readonly List<int[]> cola = new List<int[]>();
        private static bool hilosIniciados;
        private static HttpClient cliente;

        private const int MaxEnMemoria = 500;
        private static readonly TimeSpan EsperaReintento = TimeSpan.FromSeconds(20);

        // Devuelve el mosaico si ya está en memoria; si no, inicia su descarga y devuelve null.
        public static Bitmap Obtener(int z, int x, int y)
        {
            string clave = z + "/" + x + "/" + y;
            lock (cerrojo)
            {
                Bitmap b;
                if (cache.TryGetValue(clave, out b)) return b;

                DateTime cuando;
                if (fallidos.TryGetValue(clave, out cuando))
                {
                    if (DateTime.UtcNow - cuando < EsperaReintento) return null;
                    fallidos.Remove(clave);
                }
                if (pendientes.Contains(clave)) return null;
                pendientes.Add(clave);
            }

            lock (cola)
            {
                // Si se acumulan solicitudes viejas (el usuario se movió), se descartan las más antiguas.
                while (cola.Count >= MaxCola)
                {
                    int[] vieja = cola[0];
                    cola.RemoveAt(0);
                    lock (cerrojo) { pendientes.Remove(vieja[0] + "/" + vieja[1] + "/" + vieja[2]); }
                }
                cola.Add(new int[] { z, x, y });
                IniciarHilos();
                Monitor.Pulse(cola);
            }
            return null;
        }

        private static void IniciarHilos()
        {
            if (hilosIniciados) return;
            hilosIniciados = true;
            for (int i = 0; i < Trabajadores; i++)
            {
                Thread t = new Thread(Trabajar);
                t.IsBackground = true;
                t.Name = "Mosaicos-" + i;
                t.Start();
            }
        }

        // Cada hilo toma primero la solicitud más reciente (lo que el usuario está viendo ahora).
        private static void Trabajar()
        {
            while (true)
            {
                int[] t;
                lock (cola)
                {
                    while (cola.Count == 0) Monitor.Wait(cola);
                    t = cola[cola.Count - 1];
                    cola.RemoveAt(cola.Count - 1);
                }
                Cargar(t[0], t[1], t[2], t[0] + "/" + t[1] + "/" + t[2]);
            }
        }

        // ¿Hay mosaicos que fallaron hace poco? (para avisar «sin conexión»)
        public static bool HayFallosRecientes()
        {
            lock (cerrojo)
            {
                foreach (KeyValuePair<string, DateTime> par in fallidos)
                    if (DateTime.UtcNow - par.Value < EsperaReintento) return true;
                return false;
            }
        }

        public static bool HayDescargas()
        {
            lock (cerrojo) { return pendientes.Count > 0; }
        }

        private static void Cargar(int z, int x, int y, string clave)
        {
            try
            {
                string archivo = RutaEnDisco(z, x, y);
                byte[] datos = null;

                try { if (File.Exists(archivo)) datos = File.ReadAllBytes(archivo); } catch { datos = null; }

                bool deDisco = datos != null;
                if (datos == null)
                {
                    datos = ObtenerCliente().GetByteArrayAsync(UrlDe(z, x, y)).GetAwaiter().GetResult();
                }

                Bitmap bmp;
                using (MemoryStream ms = new MemoryStream(datos))
                using (Bitmap temporal = new Bitmap(ms))
                    bmp = new Bitmap(temporal);   // copia independiente del flujo

                if (!deDisco)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(archivo));
                        File.WriteAllBytes(archivo, datos);
                    }
                    catch { /* guardar en disco es opcional */ }
                }

                lock (cerrojo)
                {
                    if (cache.Count >= MaxEnMemoria) cache.Clear();
                    cache[clave] = bmp;
                }
            }
            catch (Exception ex)
            {
                UltimoError = ex.GetType().Name + ": " + ex.Message;
                lock (cerrojo) { fallidos[clave] = DateTime.UtcNow; }
            }
            finally
            {
                lock (cerrojo) { pendientes.Remove(clave); }
                Action a = Actualizado;
                if (a != null) a();
            }
        }

        private static string UrlDe(int z, int x, int y)
        {
            return UrlMosaicos.Replace("{z}", z.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());
        }

        private static HttpClient ObtenerCliente()
        {
            lock (cerrojo)
            {
                if (cliente == null)
                {
                    cliente = new HttpClient();
                    cliente.Timeout = TimeSpan.FromSeconds(8);
                    cliente.DefaultRequestHeaders.UserAgent.TryParseAdd("Proyecto2EstructurasDatosII/1.0");
                    cliente.DefaultRequestHeaders.UserAgent.TryParseAdd("(proyecto universitario de Estructuras de Datos II)");
                }
                return cliente;
            }
        }

        // Carpeta de caché: una subcarpeta por servidor, para no mezclar proveedores.
        private static string RutaEnDisco(int z, int x, int y)
        {
            string host = "osm";
            try { host = new Uri(UrlMosaicos.Replace("{z}", "0").Replace("{x}", "0").Replace("{y}", "0")).Host; } catch { }
            foreach (char c in Path.GetInvalidFileNameChars()) host = host.Replace(c, '_');

            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(baseDir)) baseDir = Path.GetTempPath();
            return Path.Combine(baseDir, "Proyecto2EstructurasDatosII", "mosaicos", host,
                                z.ToString(), x.ToString(), y + ".png");
        }
    }
}
