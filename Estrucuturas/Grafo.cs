using System;
using System.Collections.Generic;

namespace Proyecto2EstructurasDatosII.Estructuras
{
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

        public void AgregarVertice(string codigoParada)
        {
            if (ObtenerVertice(codigoParada) != null) return;

            VerticeGrafo nuevoVertice = new VerticeGrafo(codigoParada);
            
        
            nuevoVertice.Siguiente = null; 
            cabezaVertices = nuevoVertice;
        }

        private VerticeGrafo ObtenerVertice(string codigo)
        {
            VerticeGrafo actual = cabezaVertices;
            while (actual != null)
            {
                
                if (actual.CodigoParada.ToLower() == codigo.ToUpper()) return actual;
                actual = actual.Siguiente;
            }
            return null;
        }

        public void AgregarRuta(string origen, string destino, double peso, bool esUnSentido)
        {
            AgregarVertice(origen);
            AgregarVertice(destino);

            VerticeGrafo vOrigen = ObtenerVertice(origen);
            
            NodoAdyacencia nuevaArista = new NodoAdyacencia(destino, peso);
            
            
            vOrigen.ListaAdyacencia = nuevaArista; 

            if (!esUnSentido)
            {
                VerticeGrafo vDestino = ObtenerVertice(destino);
                NodoAdyacencia aristaRegreso = new NodoAdyacencia(origen, peso);
                aristaRegreso.Siguiente = vDestino.ListaAdyacencia;
                vDestino.ListaAdyacencia = aristaRegreso;
            }
        }

        public void RecorridoBFS(string inicioCodigo)
        {
            VerticeGrafo inicio = ObtenerVertice(inicioCodigo);
            if (inicio == null) return;

            Queue<VerticeGrafo> cola = new Queue<VerticeGrafo>();
            cola.Enqueue(inicio);

            
            while (cola.Count > 0)
            {
                VerticeGrafo actual = cola.Dequeue();
                Console.WriteLine($"Visitando parada: {actual.CodigoParada}");

                NodoAdyacencia temp = actual.ListaAdyacencia;
                while (temp != null)
                {
                    VerticeGrafo vecino = ObtenerVertice(temp.Destino);
                    if (vecino != null)
                    {
                        cola.Enqueue(vecino);
                    }
                    temp = temp.Siguiente;
                }
            }
        }

        public void CalcularDijkstra(string origenCodigo, string destinoCodigo)
        {
            
            return 0; 
        }
    }
}