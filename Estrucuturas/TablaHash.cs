// Morelia 
using System;
using System.Collections.Generic;
using Proyecto2EstructurasDatosII.Models;

namespace Proyecto2EstructurasDatosII.Estructuras
{
    // Tabla Hash propia con encadenamiento para resolver colisiones.
    public class TablaHash
    {
        private class Nodo
        {
            public Parada Dato;
            public Nodo Siguiente;

            public Nodo(Parada dato)
            {
                Dato = dato;
                Siguiente = null;
            }
        }

        private Nodo[] tabla;
        private int cantidad;

        public int Cantidad
        {
            get { return cantidad; }
        }

        public TablaHash(int capacidad = 17)
        {
            if (capacidad < 3)
                capacidad = 3;

            tabla = new Nodo[capacidad];
            cantidad = 0;
        }

        // Función hash polinómica: convierte el código en un índice del arreglo.
        private int FuncionHash(string codigo)
        {
            long hash = 0;

            for (int i = 0; i < codigo.Length; i++)
                hash = (hash * 31 + codigo[i]) % tabla.Length;

            return (int)hash;
        }

        public bool Insertar(Parada parada)
        {
            if (parada == null || string.IsNullOrWhiteSpace(parada.Codigo))
                return false;

            int indice = FuncionHash(parada.Codigo);
            Nodo actual = tabla[indice];

            while (actual != null)
            {
                if (actual.Dato.Codigo.Equals(parada.Codigo,
                    StringComparison.OrdinalIgnoreCase))
                    return false;

                actual = actual.Siguiente;
            }

            Nodo nuevo = new Nodo(parada);
            nuevo.Siguiente = tabla[indice];
            tabla[indice] = nuevo;
            cantidad++;
            return true;
        }

        public Parada Buscar(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return null;

            int indice = FuncionHash(codigo.Trim());
            Nodo actual = tabla[indice];

            while (actual != null)
            {
                if (actual.Dato.Codigo.Equals(codigo.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                    return actual.Dato;

                actual = actual.Siguiente;
            }

            return null;
        }

        public bool Eliminar(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            int indice = FuncionHash(codigo.Trim());
            Nodo actual = tabla[indice];
            Nodo anterior = null;

            while (actual != null)
            {
                if (actual.Dato.Codigo.Equals(codigo.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (anterior == null)
                        tabla[indice] = actual.Siguiente;
                    else
                        anterior.Siguiente = actual.Siguiente;

                    cantidad--;
                    return true;
                }

                anterior = actual;
                actual = actual.Siguiente;
            }

            return false;
        }

        public void Mostrar()
        {
            bool hayDatos = false;

            for (int i = 0; i < tabla.Length; i++)
            {
                Nodo actual = tabla[i];

                if (actual != null)
                {
                    hayDatos = true;
                    Console.Write("Posición " + i + ": ");

                    while (actual != null)
                    {
                        Console.Write("[" + actual.Dato.Codigo + " - " +
                                      actual.Dato.Nombre + "]");

                        if (actual.Siguiente != null)
                            Console.Write(" -> ");

                        actual = actual.Siguiente;
                    }

                    Console.WriteLine();
                }
            }

            if (!hayDatos)
                Console.WriteLine("La tabla hash está vacía.");
        }
    }
}