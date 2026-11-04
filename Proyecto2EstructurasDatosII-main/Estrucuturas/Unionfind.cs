// Union-Find (conjuntos disjuntos) con compresión de camino y unión por rango.
// Se usa para saber, en O(α(n)), si dos paradas pertenecen a la misma zona conectada
// ANTES de ejecutar Dijkstra o habilitar una ruta nueva.
// Integrante responsable: ver Responsables.UnionFind (Interfaz/Contexto.cs).
using System;

namespace Proyecto2EstructurasDatosII.Estructuras
{
    public class UnionFind
    {
        private int[] padre;
        private int[] rango;
        private int tamano;        // elementos creados
        private int componentes;   // conjuntos disjuntos actuales

        public int Tamano { get { return tamano; } }
        public int Componentes { get { return componentes; } }

        public UnionFind(int capacidad = 16)
        {
            if (capacidad < 4) capacidad = 4;
            padre = new int[capacidad];
            rango = new int[capacidad];
            tamano = 0;
            componentes = 0;
        }

        // Crea un conjunto nuevo con un solo elemento y devuelve su índice.
        public int CrearConjunto()
        {
            if (tamano == padre.Length)
            {
                int[] nuevoPadre = new int[padre.Length * 2];
                int[] nuevoRango = new int[rango.Length * 2];
                for (int i = 0; i < tamano; i++)
                {
                    nuevoPadre[i] = padre[i];
                    nuevoRango[i] = rango[i];
                }
                padre = nuevoPadre;
                rango = nuevoRango;
            }

            padre[tamano] = tamano;
            rango[tamano] = 0;
            componentes++;
            return tamano++;
        }

        // Find con compresión de camino (dos pasadas): todos los nodos visitados
        // quedan apuntando directamente a la raíz.
        public int Buscar(int x)
        {
            if (x < 0 || x >= tamano) return -1;

            int raiz = x;
            while (padre[raiz] != raiz) raiz = padre[raiz];

            while (padre[x] != raiz)
            {
                int siguiente = padre[x];
                padre[x] = raiz;
                x = siguiente;
            }
            return raiz;
        }

        // Union por rango. Devuelve false si ya estaban en el mismo conjunto
        // (o si algún índice no existe).
        public bool Unir(int a, int b)
        {
            int ra = Buscar(a);
            int rb = Buscar(b);
            if (ra < 0 || rb < 0 || ra == rb) return false;

            if (rango[ra] < rango[rb]) padre[ra] = rb;
            else if (rango[ra] > rango[rb]) padre[rb] = ra;
            else
            {
                padre[rb] = ra;
                rango[ra]++;
            }
            componentes--;
            return true;
        }

        public bool Conectados(int a, int b)
        {
            int ra = Buscar(a);
            return ra >= 0 && ra == Buscar(b);
        }

        // Consultas sin modificar la estructura (para mostrarla en la interfaz).
        public int PadreDe(int i) { return (i < 0 || i >= tamano) ? -1 : padre[i]; }
        public int RangoDe(int i) { return (i < 0 || i >= tamano) ? -1 : rango[i]; }
    }
}
