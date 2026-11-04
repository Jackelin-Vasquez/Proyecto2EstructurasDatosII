// Grafo dirigido con lista de adyacencia propia (listas enlazadas).
// BFS (con Queue<T> solo como control de orden de visita), DFS recursivo y
// Dijkstra con un montículo binario mínimo implementado a mano.
// Integrante responsable: ver Responsables.Grafo (Interfaz/Contexto.cs).
using System;
using System.Collections.Generic;

namespace Proyecto2EstructurasDatosII.Estructuras
{
    // Criterio de optimización de Dijkstra.
    public enum Criterio
    {
        Tiempo,
        Costo
    }

    // Datos de una arista, para mostrarla (mapa, tablas, detalle de parada).
    public class AristaInfo
    {
        public string Origen;
        public string Destino;
        public double Tiempo;
        public double Costo;
    }

    // Resultado de Dijkstra.
    public class ResultadoRuta
    {
        public bool Existe;
        public string[] Camino = new string[0];   // códigos, del origen al destino
        public double TiempoTotal;
        public double CostoTotal;
        public int VerticesProcesados;            // vértices extraídos del montículo
    }

    // Nodo para la lista de adyacencia (arista saliente).
    public class NodoAdyacencia
    {
        public string Destino { get; set; }
        public VerticeGrafo Vertice { get; set; }   // acceso directo al vértice destino
        public double Tiempo { get; set; }
        public double Costo { get; set; }
        public NodoAdyacencia Siguiente { get; set; }

        public NodoAdyacencia(string destino, VerticeGrafo vertice, double tiempo, double costo)
        {
            Destino = destino;
            Vertice = vertice;
            Tiempo = tiempo;
            Costo = costo;
            Siguiente = null;
        }
    }

    // Nodo principal del vértice (parada en el grafo).
    public class VerticeGrafo
    {
        public string CodigoParada { get; set; }
        public NodoAdyacencia ListaAdyacencia { get; set; }
        public VerticeGrafo Siguiente { get; set; }
        public int Marca;   // índice temporal usado por los recorridos

        public VerticeGrafo(string codigoParada)
        {
            CodigoParada = codigoParada;
            ListaAdyacencia = null;
            Siguiente = null;
        }
    }

    public class Grafo
    {
        private VerticeGrafo cabezaVertices;
        private VerticeGrafo colaVertices;
        private int cantidadVertices;
        private int cantidadAristas;

        public int CantidadVertices { get { return cantidadVertices; } }
        public int CantidadAristas { get { return cantidadAristas; } }

        public Grafo()
        {
            cabezaVertices = null;
            colaVertices = null;
        }

        // ------------------------------------------------------------------
        //  Vértices
        // ------------------------------------------------------------------

        // Agregar una parada (vértice) al grafo. Se enlaza al final para conservar el orden de registro.
        public void AgregarVertice(string codigoParada)
        {
            if (ObtenerVertice(codigoParada) != null) return;

            VerticeGrafo nuevo = new VerticeGrafo(codigoParada);
            if (cabezaVertices == null) cabezaVertices = nuevo;
            else colaVertices.Siguiente = nuevo;
            colaVertices = nuevo;
            cantidadVertices++;
        }

        private VerticeGrafo ObtenerVertice(string codigo)
        {
            VerticeGrafo actual = cabezaVertices;
            while (actual != null)
            {
                if (actual.CodigoParada == codigo) return actual;
                actual = actual.Siguiente;
            }
            return null;
        }

        public bool ExisteVertice(string codigo)
        {
            return ObtenerVertice(codigo) != null;
        }

        // Elimina el vértice y todas las aristas que entran o salen de él.
        public bool EliminarVertice(string codigo)
        {
            VerticeGrafo objetivo = ObtenerVertice(codigo);
            if (objetivo == null) return false;

            // Aristas que salen del vértice.
            NodoAdyacencia a = objetivo.ListaAdyacencia;
            while (a != null) { cantidadAristas--; a = a.Siguiente; }
            objetivo.ListaAdyacencia = null;

            // Aristas que llegan al vértice, y desenlazarlo de la lista de vértices.
            VerticeGrafo anterior = null;
            VerticeGrafo v = cabezaVertices;
            while (v != null)
            {
                if (v != objetivo) QuitarAristaDe(v, codigo);
                else
                {
                    // se desenlaza más abajo
                }
                v = v.Siguiente;
            }

            v = cabezaVertices;
            while (v != null && v != objetivo)
            {
                anterior = v;
                v = v.Siguiente;
            }
            if (anterior == null) cabezaVertices = objetivo.Siguiente;
            else anterior.Siguiente = objetivo.Siguiente;
            if (colaVertices == objetivo) colaVertices = anterior;

            cantidadVertices--;
            return true;
        }

        // ------------------------------------------------------------------
        //  Aristas
        // ------------------------------------------------------------------

        // Agregar una ruta (arista) entre dos paradas. Si no es de un solo sentido
        // se agrega también la arista de regreso.
        public void AgregarRuta(string origen, string destino, double tiempo, double costo, bool esUnSentido)
        {
            AgregarVertice(origen);
            AgregarVertice(destino);

            VerticeGrafo vOrigen = ObtenerVertice(origen);
            VerticeGrafo vDestino = ObtenerVertice(destino);

            AgregarAlFinal(vOrigen, new NodoAdyacencia(destino, vDestino, tiempo, costo));
            cantidadAristas++;

            if (!esUnSentido)
            {
                AgregarAlFinal(vDestino, new NodoAdyacencia(origen, vOrigen, tiempo, costo));
                cantidadAristas++;
            }
        }

        private static void AgregarAlFinal(VerticeGrafo v, NodoAdyacencia nueva)
        {
            if (v.ListaAdyacencia == null) { v.ListaAdyacencia = nueva; return; }
            NodoAdyacencia a = v.ListaAdyacencia;
            while (a.Siguiente != null) a = a.Siguiente;
            a.Siguiente = nueva;
        }

        public bool ExisteArista(string origen, string destino)
        {
            VerticeGrafo v = ObtenerVertice(origen);
            if (v == null) return false;
            for (NodoAdyacencia a = v.ListaAdyacencia; a != null; a = a.Siguiente)
                if (a.Destino == destino) return true;
            return false;
        }

        // Quita la arista origen→destino (y la inversa si la ruta era bidireccional).
        public void EliminarRuta(string origen, string destino, bool eraBidireccional)
        {
            VerticeGrafo vo = ObtenerVertice(origen);
            VerticeGrafo vd = ObtenerVertice(destino);
            if (vo != null) QuitarAristaDe(vo, destino);
            if (eraBidireccional && vd != null) QuitarAristaDe(vd, origen);
        }

        private void QuitarAristaDe(VerticeGrafo v, string destino)
        {
            NodoAdyacencia anterior = null;
            NodoAdyacencia a = v.ListaAdyacencia;
            while (a != null)
            {
                if (a.Destino == destino)
                {
                    if (anterior == null) v.ListaAdyacencia = a.Siguiente;
                    else anterior.Siguiente = a.Siguiente;
                    cantidadAristas--;
                    return;
                }
                anterior = a;
                a = a.Siguiente;
            }
        }

        // Todas las aristas dirigidas del grafo.
        public AristaInfo[] ObtenerAristas()
        {
            AristaInfo[] resultado = new AristaInfo[cantidadAristas];
            int n = 0;
            for (VerticeGrafo v = cabezaVertices; v != null; v = v.Siguiente)
                for (NodoAdyacencia a = v.ListaAdyacencia; a != null && n < resultado.Length; a = a.Siguiente)
                    resultado[n++] = Crear(v.CodigoParada, a);
            return resultado;
        }

        // Aristas que salen de un vértice.
        public AristaInfo[] ObtenerAdyacentes(string codigo)
        {
            VerticeGrafo v = ObtenerVertice(codigo);
            if (v == null) return new AristaInfo[0];

            int total = 0;
            for (NodoAdyacencia a = v.ListaAdyacencia; a != null; a = a.Siguiente) total++;

            AristaInfo[] resultado = new AristaInfo[total];
            int n = 0;
            for (NodoAdyacencia a = v.ListaAdyacencia; a != null; a = a.Siguiente)
                resultado[n++] = Crear(codigo, a);
            return resultado;
        }

        private static AristaInfo Crear(string origen, NodoAdyacencia a)
        {
            AristaInfo info = new AristaInfo();
            info.Origen = origen;
            info.Destino = a.Destino;
            info.Tiempo = a.Tiempo;
            info.Costo = a.Costo;
            return info;
        }

        // Una línea por vértice: «P01 → P04 (6 min · Q2.00), P06 …».
        public string[] DescribirAdyacencia()
        {
            string[] lineas = new string[cantidadVertices];
            int n = 0;
            for (VerticeGrafo v = cabezaVertices; v != null; v = v.Siguiente)
            {
                string linea = v.CodigoParada + "  →  ";
                if (v.ListaAdyacencia == null) linea += "(sin rutas salientes)";
                for (NodoAdyacencia a = v.ListaAdyacencia; a != null; a = a.Siguiente)
                {
                    linea += a.Destino + " (" + a.Tiempo.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) +
                             " min · Q" + a.Costo.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ")";
                    if (a.Siguiente != null) linea += ",  ";
                }
                lineas[n++] = linea;
            }
            return lineas;
        }

        // ------------------------------------------------------------------
        //  Recorridos
        // ------------------------------------------------------------------

        // Asigna a cada vértice un índice 0..V-1 en Marca y los devuelve en un arreglo.
        private VerticeGrafo[] Indexar()
        {
            VerticeGrafo[] arreglo = new VerticeGrafo[cantidadVertices];
            int i = 0;
            for (VerticeGrafo v = cabezaVertices; v != null; v = v.Siguiente)
            {
                v.Marca = i;
                arreglo[i++] = v;
            }
            return arreglo;
        }

        // BFS (anchura): devuelve los códigos en orden de visita. Queue<T> solo
        // controla el orden de visita, tal como permite el enunciado.
        public string[] RecorridoBFS(string inicioCodigo)
        {
            VerticeGrafo inicio = ObtenerVertice(inicioCodigo);
            if (inicio == null) return new string[0];

            VerticeGrafo[] todos = Indexar();
            bool[] visitado = new bool[todos.Length];
            string[] orden = new string[todos.Length];
            int n = 0;

            Queue<VerticeGrafo> cola = new Queue<VerticeGrafo>();
            visitado[inicio.Marca] = true;
            cola.Enqueue(inicio);

            while (cola.Count > 0)
            {
                VerticeGrafo actual = cola.Dequeue();
                orden[n++] = actual.CodigoParada;

                for (NodoAdyacencia a = actual.ListaAdyacencia; a != null; a = a.Siguiente)
                {
                    if (!visitado[a.Vertice.Marca])
                    {
                        visitado[a.Vertice.Marca] = true;
                        cola.Enqueue(a.Vertice);
                    }
                }
            }
            return Recortar(orden, n);
        }

        // DFS (profundidad) recursivo.
        public string[] RecorridoDFS(string inicioCodigo)
        {
            VerticeGrafo inicio = ObtenerVertice(inicioCodigo);
            if (inicio == null) return new string[0];

            VerticeGrafo[] todos = Indexar();
            bool[] visitado = new bool[todos.Length];
            string[] orden = new string[todos.Length];
            int n = 0;
            VisitarDFS(inicio, visitado, orden, ref n);
            return Recortar(orden, n);
        }

        private void VisitarDFS(VerticeGrafo v, bool[] visitado, string[] orden, ref int n)
        {
            visitado[v.Marca] = true;
            orden[n++] = v.CodigoParada;
            for (NodoAdyacencia a = v.ListaAdyacencia; a != null; a = a.Siguiente)
                if (!visitado[a.Vertice.Marca])
                    VisitarDFS(a.Vertice, visitado, orden, ref n);
        }

        private static string[] Recortar(string[] origen, int cantidad)
        {
            string[] r = new string[cantidad];
            for (int i = 0; i < cantidad; i++) r[i] = origen[i];
            return r;
        }

        // ------------------------------------------------------------------
        //  Dijkstra
        // ------------------------------------------------------------------

        // Ruta de menor tiempo o menor costo respetando el sentido de las rutas.
        public ResultadoRuta CalcularDijkstra(string origenCodigo, string destinoCodigo, Criterio criterio)
        {
            ResultadoRuta resultado = new ResultadoRuta();
            VerticeGrafo origen = ObtenerVertice(origenCodigo);
            VerticeGrafo destino = ObtenerVertice(destinoCodigo);
            if (origen == null || destino == null) return resultado;

            VerticeGrafo[] todos = Indexar();
            int V = todos.Length;
            double[] dist = new double[V];
            int[] previo = new int[V];
            NodoAdyacencia[] aristaPrevia = new NodoAdyacencia[V];
            bool[] definitivo = new bool[V];
            for (int i = 0; i < V; i++) { dist[i] = double.PositiveInfinity; previo[i] = -1; }

            MonticuloMin monticulo = new MonticuloMin(V + 4);
            dist[origen.Marca] = 0;
            monticulo.Insertar(origen.Marca, 0);

            while (monticulo.Cantidad > 0)
            {
                int u;
                double d;
                monticulo.ExtraerMinimo(out u, out d);
                if (definitivo[u]) continue;          // entrada obsoleta del montículo
                definitivo[u] = true;
                resultado.VerticesProcesados++;
                if (u == destino.Marca) break;

                for (NodoAdyacencia a = todos[u].ListaAdyacencia; a != null; a = a.Siguiente)
                {
                    int w = a.Vertice.Marca;
                    if (definitivo[w]) continue;
                    double nueva = d + (criterio == Criterio.Tiempo ? a.Tiempo : a.Costo);
                    if (nueva < dist[w])
                    {
                        dist[w] = nueva;
                        previo[w] = u;
                        aristaPrevia[w] = a;
                        monticulo.Insertar(w, nueva);
                    }
                }
            }

            if (double.IsPositiveInfinity(dist[destino.Marca])) return resultado;

            // Reconstruir el camino de atrás hacia adelante.
            int largo = 1;
            for (int x = destino.Marca; previo[x] != -1; x = previo[x]) largo++;

            string[] camino = new string[largo];
            int pos = largo - 1;
            for (int x = destino.Marca; x != -1; x = previo[x])
            {
                camino[pos--] = todos[x].CodigoParada;
                if (aristaPrevia[x] != null)
                {
                    resultado.TiempoTotal += aristaPrevia[x].Tiempo;
                    resultado.CostoTotal += aristaPrevia[x].Costo;
                }
            }

            resultado.Existe = true;
            resultado.Camino = camino;
            return resultado;
        }

        // Montículo binario mínimo (par vértice–prioridad), con crecimiento dinámico.
        private class MonticuloMin
        {
            private int[] vertices;
            private double[] claves;
            private int cantidad;

            public int Cantidad { get { return cantidad; } }

            public MonticuloMin(int capacidad)
            {
                vertices = new int[capacidad];
                claves = new double[capacidad];
            }

            public void Insertar(int vertice, double clave)
            {
                if (cantidad == vertices.Length)
                {
                    int[] v2 = new int[vertices.Length * 2];
                    double[] c2 = new double[claves.Length * 2];
                    for (int i = 0; i < cantidad; i++) { v2[i] = vertices[i]; c2[i] = claves[i]; }
                    vertices = v2;
                    claves = c2;
                }

                int hijo = cantidad++;
                while (hijo > 0)
                {
                    int padre = (hijo - 1) / 2;
                    if (claves[padre] <= clave) break;
                    vertices[hijo] = vertices[padre];
                    claves[hijo] = claves[padre];
                    hijo = padre;
                }
                vertices[hijo] = vertice;
                claves[hijo] = clave;
            }

            public void ExtraerMinimo(out int vertice, out double clave)
            {
                vertice = vertices[0];
                clave = claves[0];

                cantidad--;
                if (cantidad == 0) return;

                int vUlt = vertices[cantidad];
                double cUlt = claves[cantidad];
                int padre = 0;
                while (true)
                {
                    int hijo = 2 * padre + 1;
                    if (hijo >= cantidad) break;
                    if (hijo + 1 < cantidad && claves[hijo + 1] < claves[hijo]) hijo++;
                    if (claves[hijo] >= cUlt) break;
                    vertices[padre] = vertices[hijo];
                    claves[padre] = claves[hijo];
                    padre = hijo;
                }
                vertices[padre] = vUlt;
                claves[padre] = cUlt;
            }
        }
    }
}
