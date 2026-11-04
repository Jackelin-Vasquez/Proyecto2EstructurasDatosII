// Trie (árbol de prefijos) para el autocompletado de nombres y códigos de parada.
// Cada nodo guarda un carácter; los hijos de un nodo forman una lista enlazada ordenada
// alfabéticamente (sin Dictionary), por lo que las sugerencias salen ya ordenadas.
// Integrante responsable: ver Responsables.Trie (Interfaz/Contexto.cs).
using System;
using System.Globalization;
using System.Text;

namespace Proyecto2EstructurasDatosII.Estructuras
{
    // Una sugerencia: el texto original indexado y el código de la parada a la que apunta.
    public class Sugerencia
    {
        public string Texto;
        public string Codigo;

        public Sugerencia(string texto, string codigo)
        {
            Texto = texto;
            Codigo = codigo;
        }
    }

    public class Trie
    {
        private class NodoTrie
        {
            public char Letra;
            public NodoTrie Hijo;        // primer hijo
            public NodoTrie Hermano;     // siguiente hijo del mismo padre (orden alfabético)
            public Sugerencia[] Entradas; // palabras que terminan aquí (pueden repetirse nombres)
            public int NumEntradas;

            public NodoTrie(char letra) { Letra = letra; }
        }

        private readonly NodoTrie raiz = new NodoTrie('\0');
        private int cantidad;

        public int Cantidad { get { return cantidad; } }

        // Minúsculas y sin tildes, para que «cafe» encuentre «Café».
        public static string Normalizar(string texto)
        {
            if (texto == null) return "";
            string d = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder(d.Length);
            for (int i = 0; i < d.Length; i++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(d[i]) != UnicodeCategory.NonSpacingMark)
                    sb.Append(d[i]);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------
        //  Inserción
        // ------------------------------------------------------------------

        public bool Insertar(string texto, string codigo)
        {
            string clave = Normalizar(texto);
            if (clave.Length == 0 || codigo == null) return false;

            NodoTrie actual = raiz;
            for (int i = 0; i < clave.Length; i++)
                actual = ObtenerOCrearHijo(actual, clave[i]);

            for (int i = 0; i < actual.NumEntradas; i++)
                if (actual.Entradas[i].Codigo == codigo) return false;

            if (actual.Entradas == null) actual.Entradas = new Sugerencia[1];
            else if (actual.NumEntradas == actual.Entradas.Length)
            {
                Sugerencia[] mayor = new Sugerencia[actual.Entradas.Length * 2];
                for (int i = 0; i < actual.NumEntradas; i++) mayor[i] = actual.Entradas[i];
                actual.Entradas = mayor;
            }
            actual.Entradas[actual.NumEntradas++] = new Sugerencia(texto.Trim(), codigo);
            cantidad++;
            return true;
        }

        // Inserta el hijo en su posición alfabética dentro de la lista de hermanos.
        private NodoTrie ObtenerOCrearHijo(NodoTrie padre, char letra)
        {
            NodoTrie anterior = null;
            NodoTrie actual = padre.Hijo;
            while (actual != null && actual.Letra < letra)
            {
                anterior = actual;
                actual = actual.Hermano;
            }
            if (actual != null && actual.Letra == letra) return actual;

            NodoTrie nuevo = new NodoTrie(letra);
            nuevo.Hermano = actual;
            if (anterior == null) padre.Hijo = nuevo;
            else anterior.Hermano = nuevo;
            return nuevo;
        }

        private static NodoTrie BuscarHijo(NodoTrie padre, char letra)
        {
            NodoTrie actual = padre.Hijo;
            while (actual != null && actual.Letra < letra) actual = actual.Hermano;
            return (actual != null && actual.Letra == letra) ? actual : null;
        }

        private NodoTrie NodoDe(string clave)
        {
            NodoTrie actual = raiz;
            for (int i = 0; i < clave.Length && actual != null; i++)
                actual = BuscarHijo(actual, clave[i]);
            return actual;
        }

        // ------------------------------------------------------------------
        //  Búsqueda
        // ------------------------------------------------------------------

        // ¿Existe exactamente esa palabra (con ese código, si se indica)?
        public bool Contiene(string texto, string codigo = null)
        {
            NodoTrie n = NodoDe(Normalizar(texto));
            if (n == null || n.NumEntradas == 0) return false;
            if (codigo == null) return true;
            for (int i = 0; i < n.NumEntradas; i++)
                if (n.Entradas[i].Codigo == codigo) return true;
            return false;
        }

        // Búsqueda por prefijo: hasta «maximo» sugerencias, en orden alfabético.
        public Sugerencia[] BuscarPorPrefijo(string prefijo, int maximo)
        {
            if (maximo <= 0 || cantidad == 0) return new Sugerencia[0];
            NodoTrie inicio = NodoDe(Normalizar(prefijo));
            if (inicio == null) return new Sugerencia[0];

            Sugerencia[] buffer = new Sugerencia[Math.Min(maximo, cantidad)];
            int n = 0;
            Recolectar(inicio, buffer, ref n);

            Sugerencia[] resultado = new Sugerencia[n];
            for (int i = 0; i < n; i++) resultado[i] = buffer[i];
            return resultado;
        }

        public Sugerencia[] ObtenerTodas()
        {
            return BuscarPorPrefijo("", cantidad);
        }

        private void Recolectar(NodoTrie nodo, Sugerencia[] buffer, ref int n)
        {
            for (int i = 0; i < nodo.NumEntradas && n < buffer.Length; i++)
                buffer[n++] = nodo.Entradas[i];

            NodoTrie hijo = nodo.Hijo;
            while (hijo != null && n < buffer.Length)
            {
                Recolectar(hijo, buffer, ref n);
                hijo = hijo.Hermano;
            }
        }

        // ------------------------------------------------------------------
        //  Eliminación
        // ------------------------------------------------------------------

        public bool Eliminar(string texto, string codigo)
        {
            string clave = Normalizar(texto);
            if (clave.Length == 0) return false;
            bool borrado = false;
            EliminarRec(raiz, clave, 0, codigo, ref borrado);
            return borrado;
        }

        // Devuelve true si el nodo quedó sin utilidad y su padre puede desenlazarlo.
        private bool EliminarRec(NodoTrie nodo, string clave, int pos, string codigo, ref bool borrado)
        {
            if (pos == clave.Length)
            {
                for (int i = 0; i < nodo.NumEntradas; i++)
                {
                    if (nodo.Entradas[i].Codigo != codigo) continue;
                    for (int j = i; j < nodo.NumEntradas - 1; j++) nodo.Entradas[j] = nodo.Entradas[j + 1];
                    nodo.Entradas[--nodo.NumEntradas] = null;
                    cantidad--;
                    borrado = true;
                    break;
                }
            }
            else
            {
                NodoTrie anterior = null;
                NodoTrie hijo = nodo.Hijo;
                while (hijo != null && hijo.Letra != clave[pos])
                {
                    anterior = hijo;
                    hijo = hijo.Hermano;
                }
                if (hijo != null && EliminarRec(hijo, clave, pos + 1, codigo, ref borrado))
                {
                    if (anterior == null) nodo.Hijo = hijo.Hermano;
                    else anterior.Hermano = hijo.Hermano;
                }
            }
            return nodo != raiz && nodo.NumEntradas == 0 && nodo.Hijo == null;
        }

        // ------------------------------------------------------------------
        //  Información e impresión
        // ------------------------------------------------------------------

        public int ContarNodos()
        {
            return ContarRec(raiz);
        }

        private int ContarRec(NodoTrie nodo)
        {
            int total = 1;
            NodoTrie h = nodo.Hijo;
            while (h != null)
            {
                total += ContarRec(h);
                h = h.Hermano;
            }
            return total;
        }

        // Imprime el árbol (hasta «maxLineas» líneas). Las cadenas sin bifurcación se
        // muestran juntas en una sola línea; ● marca el final de una palabra.
        public string[] Imprimir(int maxLineas)
        {
            if (cantidad == 0 || maxLineas <= 0) return new string[0];
            string[] lineas = new string[maxLineas];
            int n = 0;
            ImprimirHijos(raiz, "", lineas, ref n);
            string[] resultado = new string[n];
            for (int i = 0; i < n; i++) resultado[i] = lineas[i];
            return resultado;
        }

        private void ImprimirHijos(NodoTrie nodo, string prefijo, string[] lineas, ref int n)
        {
            NodoTrie hijo = nodo.Hijo;
            while (hijo != null && n < lineas.Length)
            {
                bool ultimo = hijo.Hermano == null;

                // Fusionar cadena: mientras no haya palabra ni bifurcación, seguir bajando.
                StringBuilder etiqueta = new StringBuilder();
                etiqueta.Append(hijo.Letra);
                NodoTrie fin = hijo;
                while (fin.NumEntradas == 0 && fin.Hijo != null && fin.Hijo.Hermano == null)
                {
                    fin = fin.Hijo;
                    etiqueta.Append(fin.Letra);
                }

                string linea = prefijo + (ultimo ? "└─ " : "├─ ") + etiqueta;
                if (fin.NumEntradas > 0)
                {
                    linea += "  ●";
                    for (int i = 0; i < fin.NumEntradas; i++)
                        linea += (i == 0 ? " → " : ", ") + fin.Entradas[i].Codigo;
                }
                lineas[n++] = linea;

                ImprimirHijos(fin, prefijo + (ultimo ? "   " : "│  "), lineas, ref n);
                hijo = hijo.Hermano;
            }
        }
    }
}
