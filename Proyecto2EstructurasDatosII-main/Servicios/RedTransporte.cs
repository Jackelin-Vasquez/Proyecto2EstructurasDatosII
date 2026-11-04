// Fachada que coordina las cuatro estructuras en un mismo flujo:
//   Trie        -> sugiere / resuelve el nombre o código que escribe el usuario
//   Tabla Hash  -> localiza el registro completo de la parada por su código
//   Union-Find  -> valida que las paradas estén en la misma zona conectada
//   Grafo       -> BFS, DFS y Dijkstra (ruta más rápida o más económica)
using System;
using Proyecto2EstructurasDatosII.Estructuras;
using Proyecto2EstructurasDatosII.Models;

namespace Proyecto2EstructurasDatosII.Servicios
{
    // Un grupo de paradas conectadas entre sí (resultado del Union-Find).
    public class Zona
    {
        public int Numero;          // 1, 2, 3...
        public int Raiz;            // índice raíz en el Union-Find
        public Parada[] Paradas;
    }

    // Resultado completo de un cálculo de ruta, con la traza de qué estructura hizo qué.
    public class ResultadoFlujo
    {
        public string Error;                 // null si todo salió bien
        public Parada Origen;
        public Parada Destino;
        public bool MismaZona;
        public ResultadoRuta Ruta;
        public string[] Traza = new string[0];
    }

    public class RedTransporte
    {
        public TablaHash Paradas { get; private set; }
        public Grafo Grafo { get; private set; }
        public Trie TrieNombres { get; private set; }
        public Trie TrieCodigos { get; private set; }
        public UnionFind Zonas { get; private set; }

        private Ruta[] rutas;
        private int cantidadRutas;

        public int CantidadParadas { get { return Paradas.Cantidad; } }
        public int CantidadRutas { get { return cantidadRutas; } }

        public RedTransporte()
        {
            Reiniciar();
        }

        public void Reiniciar()
        {
            Paradas = new TablaHash();
            Grafo = new Grafo();
            TrieNombres = new Trie();
            TrieCodigos = new Trie();
            Zonas = new UnionFind(16);
            rutas = new Ruta[16];
            cantidadRutas = 0;
        }

        public static string NormalizarCodigo(string codigo)
        {
            return codigo == null ? "" : codigo.Trim().ToUpperInvariant();
        }

        // ------------------------------------------------------------------
        //  Paradas
        // ------------------------------------------------------------------

        // Devuelve null si se registró, o el mensaje de error.
        public string RegistrarParada(string codigo, string nombre, double latitud, double longitud)
        {
            string cod = NormalizarCodigo(codigo);
            string nom = nombre == null ? "" : nombre.Trim();

            if (cod.Length == 0) return "El código de la parada es obligatorio.";
            if (nom.Length == 0) return "El nombre de la parada es obligatorio.";
            if (cod.IndexOf(',') >= 0 || nom.IndexOf(',') >= 0)
                return "El código y el nombre no pueden contener comas (formato CSV).";
            if (latitud < -90 || latitud > 90) return "La latitud debe estar entre -90 y 90.";
            if (longitud < -180 || longitud > 180) return "La longitud debe estar entre -180 y 180.";

            Parada parada = new Parada(cod, nom, latitud, longitud);
            if (!Paradas.Insertar(parada))
                return "Ya existe una parada con el código " + cod + ".";

            parada.Indice = Zonas.CrearConjunto();     // Union-Find: nueva zona de un solo elemento
            TrieNombres.Insertar(nom, cod);            // Trie: autocompletado por nombre
            TrieCodigos.Insertar(cod, cod);            // Trie: autocompletado por código
            Grafo.AgregarVertice(cod);                 // Grafo: nuevo vértice
            return null;
        }

        // Elimina la parada de TODAS las estructuras y las rutas que la tocaban.
        public string EliminarParada(string codigo, out int rutasEliminadas)
        {
            rutasEliminadas = 0;
            Parada p = Paradas.Buscar(codigo);
            if (p == null) return "La parada " + NormalizarCodigo(codigo) + " no existe.";

            // Quitar del arreglo de rutas las que usan esta parada.
            int destino = 0;
            for (int i = 0; i < cantidadRutas; i++)
            {
                if (rutas[i].Origen == p.Codigo || rutas[i].Destino == p.Codigo)
                    rutasEliminadas++;
                else
                    rutas[destino++] = rutas[i];
            }
            for (int i = destino; i < cantidadRutas; i++) rutas[i] = null;
            cantidadRutas = destino;

            Paradas.Eliminar(p.Codigo);
            TrieNombres.Eliminar(p.Nombre, p.Codigo);
            TrieCodigos.Eliminar(p.Codigo, p.Codigo);
            Grafo.EliminarVertice(p.Codigo);

            ReconstruirConectividad();
            return null;
        }

        public Parada Localizar(string codigo)
        {
            return Paradas.Buscar(codigo);
        }

        // Interpreta lo que escribió el usuario: primero como código (Hash) y luego
        // como nombre exacto (Trie). Devuelve null si no corresponde a ninguna parada.
        public Parada ResolverParada(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;

            Parada porCodigo = Paradas.Buscar(texto);
            if (porCodigo != null) return porCodigo;

            string buscado = Trie.Normalizar(texto);
            Sugerencia[] candidatas = TrieNombres.BuscarPorPrefijo(texto, 50);
            for (int i = 0; i < candidatas.Length; i++)
            {
                if (Trie.Normalizar(candidatas[i].Texto) == buscado)
                    return Paradas.Buscar(candidatas[i].Codigo);
            }
            return null;
        }

        // Autocompletado: nombres que empiezan por el prefijo y luego códigos (sin repetir paradas).
        public Parada[] Sugerir(string prefijo, int maximo)
        {
            if (string.IsNullOrWhiteSpace(prefijo) || maximo <= 0) return new Parada[0];

            Sugerencia[] porNombre = TrieNombres.BuscarPorPrefijo(prefijo, maximo);
            Sugerencia[] porCodigo = TrieCodigos.BuscarPorPrefijo(prefijo, maximo);

            Parada[] buffer = new Parada[porNombre.Length + porCodigo.Length];
            int n = 0;
            for (int i = 0; i < porNombre.Length && n < maximo; i++)
                n = AgregarSiNoEsta(buffer, n, Paradas.Buscar(porNombre[i].Codigo));
            for (int i = 0; i < porCodigo.Length && n < maximo; i++)
                n = AgregarSiNoEsta(buffer, n, Paradas.Buscar(porCodigo[i].Codigo));

            Parada[] resultado = new Parada[n];
            for (int i = 0; i < n; i++) resultado[i] = buffer[i];
            return resultado;
        }

        private static int AgregarSiNoEsta(Parada[] buffer, int n, Parada p)
        {
            if (p == null) return n;
            for (int i = 0; i < n; i++) if (buffer[i] == p) return n;
            buffer[n] = p;
            return n + 1;
        }

        // Paradas ordenadas por código (inserción; la cantidad es pequeña).
        public Parada[] ParadasOrdenadas()
        {
            Parada[] todas = Paradas.ObtenerTodas();
            for (int i = 1; i < todas.Length; i++)
            {
                Parada actual = todas[i];
                int j = i - 1;
                while (j >= 0 && string.CompareOrdinal(todas[j].Codigo, actual.Codigo) > 0)
                {
                    todas[j + 1] = todas[j];
                    j--;
                }
                todas[j + 1] = actual;
            }
            return todas;
        }

        // ------------------------------------------------------------------
        //  Rutas
        // ------------------------------------------------------------------

        // Informa si dos paradas ya están en la misma zona (Union-Find) antes de crear la ruta.
        // Devuelve null si alguna parada no existe.
        public bool? EstanEnLaMismaZona(string codigoA, string codigoB)
        {
            Parada a = Paradas.Buscar(codigoA);
            Parada b = Paradas.Buscar(codigoB);
            if (a == null || b == null) return null;
            return Zonas.Conectados(a.Indice, b.Indice);
        }

        // Devuelve null si se registró, o el mensaje de error.
        public string RegistrarRuta(string origen, string destino, double tiempo, double costo, bool bidireccional)
        {
            Parada a = Paradas.Buscar(origen);
            Parada b = Paradas.Buscar(destino);

            if (a == null) return "La parada de origen " + NormalizarCodigo(origen) + " no existe.";
            if (b == null) return "La parada de destino " + NormalizarCodigo(destino) + " no existe.";
            if (a == b) return "El origen y el destino deben ser paradas distintas.";
            if (!(tiempo > 0)) return "El tiempo debe ser mayor que cero.";
            if (!(costo >= 0)) return "El costo no puede ser negativo.";

            if (Grafo.ExisteArista(a.Codigo, b.Codigo) ||
                (bidireccional && Grafo.ExisteArista(b.Codigo, a.Codigo)))
                return "Ya existe una ruta entre " + a.Codigo + " y " + b.Codigo + " en ese sentido.";

            if (cantidadRutas == rutas.Length)
            {
                Ruta[] mayor = new Ruta[rutas.Length * 2];
                for (int i = 0; i < cantidadRutas; i++) mayor[i] = rutas[i];
                rutas = mayor;
            }

            rutas[cantidadRutas++] = new Ruta(a.Codigo, b.Codigo, tiempo, costo, bidireccional);
            Grafo.AgregarRuta(a.Codigo, b.Codigo, tiempo, costo, !bidireccional);
            Zonas.Unir(a.Indice, b.Indice);          // Union-Find: las dos zonas pasan a ser una
            return null;
        }

        public Ruta[] ObtenerRutas()
        {
            Ruta[] copia = new Ruta[cantidadRutas];
            for (int i = 0; i < cantidadRutas; i++) copia[i] = rutas[i];
            return copia;
        }

        public string EliminarRuta(int posicion)
        {
            if (posicion < 0 || posicion >= cantidadRutas) return "La ruta seleccionada no existe.";

            Ruta r = rutas[posicion];
            for (int i = posicion; i < cantidadRutas - 1; i++) rutas[i] = rutas[i + 1];
            rutas[--cantidadRutas] = null;

            Grafo.EliminarRuta(r.Origen, r.Destino, r.EsBidireccional);
            ReconstruirConectividad();
            return null;
        }

        // Union-Find no puede "separar" conjuntos, así que tras eliminar se reconstruye
        // desde cero a partir de las rutas que siguen vigentes.
        public void ReconstruirConectividad()
        {
            Zonas = new UnionFind(Paradas.Cantidad + 4);
            Parada[] todas = Paradas.ObtenerTodas();
            for (int i = 0; i < todas.Length; i++)
                todas[i].Indice = Zonas.CrearConjunto();

            for (int i = 0; i < cantidadRutas; i++)
            {
                Parada a = Paradas.Buscar(rutas[i].Origen);
                Parada b = Paradas.Buscar(rutas[i].Destino);
                if (a != null && b != null) Zonas.Unir(a.Indice, b.Indice);
            }
        }

        // ------------------------------------------------------------------
        //  Zonas conectadas
        // ------------------------------------------------------------------

        public Zona[] ObtenerZonas()
        {
            Parada[] todas = ParadasOrdenadas();
            int n = todas.Length;
            int[] raizDe = new int[n];
            int[] zonaDe = new int[n];
            int[] raicesVistas = new int[n];
            int cantidadZonas = 0;

            for (int i = 0; i < n; i++)
            {
                raizDe[i] = Zonas.Buscar(todas[i].Indice);
                int z = -1;
                for (int k = 0; k < cantidadZonas; k++)
                    if (raicesVistas[k] == raizDe[i]) { z = k; break; }
                if (z == -1)
                {
                    raicesVistas[cantidadZonas] = raizDe[i];
                    z = cantidadZonas++;
                }
                zonaDe[i] = z;
            }

            int[] tamanos = new int[cantidadZonas];
            for (int i = 0; i < n; i++) tamanos[zonaDe[i]]++;

            Zona[] zonas = new Zona[cantidadZonas];
            int[] llenado = new int[cantidadZonas];
            for (int z = 0; z < cantidadZonas; z++)
            {
                zonas[z] = new Zona();
                zonas[z].Numero = z + 1;
                zonas[z].Raiz = raicesVistas[z];
                zonas[z].Paradas = new Parada[tamanos[z]];
            }
            for (int i = 0; i < n; i++)
                zonas[zonaDe[i]].Paradas[llenado[zonaDe[i]]++] = todas[i];

            return zonas;
        }

        // Número de zona (1, 2, 3...) de cada parada del arreglo, en el mismo orden.
        public int[] NumerosDeZona(Parada[] paradas)
        {
            Zona[] zonas = ObtenerZonas();
            int[] resultado = new int[paradas.Length];
            for (int i = 0; i < paradas.Length; i++)
            {
                int raiz = Zonas.Buscar(paradas[i].Indice);
                for (int z = 0; z < zonas.Length; z++)
                {
                    if (zonas[z].Raiz == raiz) { resultado[i] = zonas[z].Numero; break; }
                }
            }
            return resultado;
        }

        // Código de la parada que tiene ese índice en el Union-Find (null si no hay).
        public string CodigoDeIndice(int indice)
        {
            Parada[] todas = Paradas.ObtenerTodas();
            for (int i = 0; i < todas.Length; i++)
                if (todas[i].Indice == indice) return todas[i].Codigo;
            return null;
        }

        // ------------------------------------------------------------------
        //  Flujo integrado: Trie -> Hash -> Union-Find -> Dijkstra
        // ------------------------------------------------------------------

        public ResultadoFlujo CalcularRuta(string textoOrigen, string textoDestino, Criterio criterio)
        {
            ResultadoFlujo flujo = new ResultadoFlujo();
            string[] traza = new string[16];
            int t = 0;

            // 1) Trie + Hash: resolver lo escrito a un registro completo.
            Parada origen = ResolverConTraza(textoOrigen, "origen", traza, ref t);
            Parada destino = ResolverConTraza(textoDestino, "destino", traza, ref t);
            flujo.Traza = Recortar(traza, t);

            if (origen == null)
            {
                flujo.Error = "No se reconoce la parada de origen «" + (textoOrigen ?? "").Trim() + "».";
                return flujo;
            }
            if (destino == null)
            {
                flujo.Error = "No se reconoce la parada de destino «" + (textoDestino ?? "").Trim() + "».";
                return flujo;
            }
            flujo.Origen = origen;
            flujo.Destino = destino;

            if (origen == destino)
            {
                flujo.Error = "El origen y el destino son la misma parada.";
                return flujo;
            }

            // 2) Union-Find: ¿están en la misma zona?
            int raizO = Zonas.Buscar(origen.Indice);
            int raizD = Zonas.Buscar(destino.Indice);
            flujo.MismaZona = raizO == raizD;
            traza[t++] = "Union-Find · Find(" + origen.Codigo + ") = " + raizO + ", Find(" + destino.Codigo + ") = " + raizD +
                         (flujo.MismaZona ? " → misma zona conectada" : " → zonas distintas");

            if (!flujo.MismaZona)
            {
                traza[t++] = "Grafo · Dijkstra omitido: no hay conexión posible.";
                flujo.Traza = Recortar(traza, t);
                flujo.Error = "Las paradas pertenecen a zonas distintas; no existe ninguna ruta entre ellas.";
                return flujo;
            }

            // 3) Grafo: Dijkstra con el criterio elegido.
            flujo.Ruta = Grafo.CalcularDijkstra(origen.Codigo, destino.Codigo, criterio);
            string nombreCriterio = criterio == Criterio.Tiempo ? "tiempo" : "costo";
            if (flujo.Ruta.Existe)
            {
                traza[t++] = "Grafo · Dijkstra (" + nombreCriterio + "): " + flujo.Ruta.VerticesProcesados +
                             " paradas procesadas, camino de " + flujo.Ruta.Camino.Length + " paradas.";
            }
            else
            {
                traza[t++] = "Grafo · Dijkstra (" + nombreCriterio + "): sin camino respetando los sentidos de las rutas.";
                flujo.Error = "Están en la misma zona, pero los sentidos únicos de las rutas impiden llegar de " +
                              origen.Codigo + " a " + destino.Codigo + ".";
            }

            flujo.Traza = Recortar(traza, t);
            return flujo;
        }

        private Parada ResolverConTraza(string texto, string rol, string[] traza, ref int t)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                traza[t++] = "Entrada · el " + rol + " está vacío.";
                return null;
            }

            Parada p = Paradas.Buscar(texto);
            if (p != null)
            {
                traza[t++] = "Tabla Hash · " + rol + " «" + p.Codigo + "» localizado en la cubeta " +
                             Paradas.IndiceCubeta(p.Codigo) + " de " + Paradas.Capacidad + ".";
                return p;
            }

            Sugerencia[] candidatas = TrieNombres.BuscarPorPrefijo(texto, 50);
            string buscado = Trie.Normalizar(texto);
            for (int i = 0; i < candidatas.Length; i++)
            {
                if (Trie.Normalizar(candidatas[i].Texto) == buscado)
                {
                    p = Paradas.Buscar(candidatas[i].Codigo);
                    traza[t++] = "Trie · «" + texto.Trim() + "» coincide con el código " + candidatas[i].Codigo + ".";
                    traza[t++] = "Tabla Hash · " + rol + " «" + p.Codigo + "» localizado en la cubeta " +
                                 Paradas.IndiceCubeta(p.Codigo) + " de " + Paradas.Capacidad + ".";
                    return p;
                }
            }

            traza[t++] = "Trie · «" + texto.Trim() + "» no coincide con ningún nombre ni código.";
            return null;
        }

        private static string[] Recortar(string[] origen, int cantidad)
        {
            string[] r = new string[cantidad];
            for (int i = 0; i < cantidad; i++) r[i] = origen[i];
            return r;
        }

        // ------------------------------------------------------------------
        //  Carga desde archivos .csv
        // ------------------------------------------------------------------

        public int CargarParadasCsv(string archivo, Action<string> registro)
        {
            Parada[] leidas = CargadorDatos.LeerParadas(archivo, registro);
            int cargadas = 0;
            for (int i = 0; i < leidas.Length; i++)
            {
                string error = RegistrarParada(leidas[i].Codigo, leidas[i].Nombre, leidas[i].Latitud, leidas[i].Longitud);
                if (error == null) cargadas++;
                else if (registro != null) registro("Parada " + leidas[i].Codigo + " omitida: " + error);
            }
            return cargadas;
        }

        public int CargarRutasCsv(string archivo, Action<string> registro)
        {
            Ruta[] buffer = new Ruta[CargadorDatos.ContarLineasDatos(archivo) + 1];
            int leidas = CargadorDatos.CargarRutas(archivo, buffer, registro);
            int cargadas = 0;
            for (int i = 0; i < leidas; i++)
            {
                string error = RegistrarRuta(buffer[i].Origen, buffer[i].Destino, buffer[i].Tiempo,
                                             buffer[i].Costo, buffer[i].EsBidireccional);
                if (error == null) cargadas++;
                else if (registro != null) registro("Ruta " + buffer[i].Origen + "→" + buffer[i].Destino + " omitida: " + error);
            }
            return cargadas;
        }
    }
}
