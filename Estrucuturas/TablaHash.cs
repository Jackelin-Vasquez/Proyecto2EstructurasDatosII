
// Morelia
using System;
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

        // Normaliza los códigos.
        private string NormalizarCodigo(string codigo)
        {
            return codigo.Trim().ToUpperInvariant();
        }

        // Función hash polinómica.
        private int FuncionHash(string codigo)
        {
            string codigoNormalizado = NormalizarCodigo(codigo);

            long hash = 0;

            for (int i = 0; i < codigoNormalizado.Length; i++)
            {
                hash = (hash * 31 + codigoNormalizado[i]) % tabla.Length;
            }

            return (int)hash;
        }

        // Insertar una parada.
        public bool Insertar(Parada parada)
        {
            if (parada == null || string.IsNullOrWhiteSpace(parada.Codigo))
                return false;

            string codigoNormalizado = NormalizarCodigo(parada.Codigo);

            int indice = FuncionHash(codigoNormalizado);
            Nodo actual = tabla[indice];

            while (actual != null)
            {
                if (NormalizarCodigo(actual.Dato.Codigo) == codigoNormalizado)
                    return false;

                actual = actual.Siguiente;
            }

            Nodo nuevo = new Nodo(parada);
            nuevo.Siguiente = tabla[indice];
            tabla[indice] = nuevo;

            cantidad++;

            return true;
        }

        // Buscar una parada por código.
        public Parada Buscar(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return null;

            string codigoNormalizado = NormalizarCodigo(codigo);

            int indice = FuncionHash(codigoNormalizado);
            Nodo actual = tabla[indice];

            while (actual != null)
            {
                if (NormalizarCodigo(actual.Dato.Codigo) == codigoNormalizado)
                    return actual.Dato;

                actual = actual.Siguiente;
            }

            return null;
        }

        // Eliminar una parada.
        public bool Eliminar(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            string codigoNormalizado = NormalizarCodigo(codigo);

            int indice = FuncionHash(codigoNormalizado);
            Nodo actual = tabla[indice];
            Nodo anterior = null;

            while (actual != null)
            {
                if (NormalizarCodigo(actual.Dato.Codigo) == codigoNormalizado)
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

        // Mostrar todas las paradas.
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