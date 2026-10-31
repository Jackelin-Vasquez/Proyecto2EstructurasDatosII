using System;
using System.Collections.Generic;

namespace Proyecto2EstructurasDatosII.Estructuras
{
    // Nodo para la lista de adyacencia (Arista)
    public class NodoAdyacencia
    {
        public string Destino { get; set; }
        public double Peso { get; set; }
        public NodoAdyacencia Siguiente { get; set; }

        public NodoAdyacencia(string destino, double peso)
        {
            Destino = destino;
            Peso = peso;
            Siguiente = null;
        }
    }

    // Nodo principal del Vértice (Parada en el Grafo)
    public class VerticeGrafo
    {
        public string CodigoParada { get; set; }
        public NodoAdyacencia ListaAdyacencia { get; set; }
        public VerticeGrafo Siguiente { get; set; }

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

        public Grafo()
        {
            cabezaVertices = null;
        }

        // Agregar una parada (vértice) al grafo
        public void AgregarVertice(string codigoParada)
        {
            if (ObtenerVertice(codigoParada) != null) return; // Ya existe

            VerticeGrafo nuevoVertice = new VerticeGrafo(codigoParada);
            nuevoVertice.Siguiente = cabezaVertices;
            cabezaVertices = nuevoVertice;
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

        // Agregar una ruta (arista) entre dos paradas
        public void AgregarRuta(string origen, string destino, double peso, bool esUnSentido)
        {
            // Asegurarnos de que ambos vértices existan
            AgregarVertice(origen);
            AgregarVertice(destino);

            VerticeGrafo vOrigen = ObtenerVertice(origen);
            
            // Añadir arista de Origen -> Destino
            NodoAdyacencia nuevaArista = new NodoAdyacencia(destino, peso);
            nuevaArista.Siguiente = vOrigen.ListaAdyacencia;
            vOrigen.ListaAdyacencia = nuevaArista;

            // Si la ruta es de doble sentido, agregamos también el regreso
            if (!esUnSentido)
            {
                VerticeGrafo vDestino = ObtenerVertice(destino);
                NodoAdyacencia aristaRegreso = new NodoAdyacencia(origen, peso);
                aristaRegreso.Siguiente = vDestino.ListaAdyacencia;
                vDestino.ListaAdyacencia = aristaRegreso;
            }
        }

        // Recorrido BFS (Ancura)
        public void RecorridoBFS(string inicioCodigo)
        {
            VerticeGrafo inicio = ObtenerVertice(inicioCodigo);
            if (inicio == null)
            {
                Console.WriteLine("Parada de inicio no encontrada en el grafo.");
                return;
            }

            // Usamos una lista simple para nodos visitados
            // Para simplificar 
            Console.WriteLine($"--- Recorrido BFS desde {inicioCodigo} ---");
            Queue<VerticeGrafo> cola = new Queue<VerticeGrafo>();
        }

        //Recorrido DFS (Profundidad)
        public void RecorridoDFS(string inicioCodigo)
        {
            Console.WriteLine($"--- Recorrido DFS desde {inicioCodigo} ---");
        }

        // Algoritmo de Dijstra
        public void CalcularDijkstra(string origenCodigo, string destinoCodigo)
        {
            Console.WriteLine($"Calculando la ruta óptima desde {origenCodigo} hasta {destinoCodigo}...");
            // Aquí se implementa la lógica de distancias mínimas y selección de nodos adyacentes 
        }
    }
}