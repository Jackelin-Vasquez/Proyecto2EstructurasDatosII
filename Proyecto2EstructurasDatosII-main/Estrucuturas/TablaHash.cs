
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

        public int Capacidad
        {
            get { return tabla.Length; }
        }

        public double FactorCarga
        {
            get { return (double)cantidad / tabla.Length; }
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

            // Si la carga supera 0.75 se duplica la capacidad (mantiene las cadenas cortas).
            if ((cantidad + 1) > tabla.Length * 3 / 4)
            {
                Redimensionar(tabla.Length * 2 + 1);
                indice = FuncionHash(codigoNormalizado);
            }

            Nodo nuevo = new Nodo(parada);
            nuevo.Siguiente = tabla[indice];
            tabla[indice] = nuevo;

            cantidad++;

            return true;
        }

        // Reubica todos los nodos en una tabla de nueva capacidad (rehash).
        private void Redimensionar(int nuevaCapacidad)
        {
            Nodo[] anterior = tabla;
            tabla = new Nodo[nuevaCapacidad];

            for (int i = 0; i < anterior.Length; i++)
            {
                Nodo actual = anterior[i];
                while (actual != null)
                {
                    Nodo siguiente = actual.Siguiente;
                    int indice = FuncionHash(actual.Dato.Codigo);
                    actual.Siguiente = tabla[indice];
                    tabla[indice] = actual;
                    actual = siguiente;
                }
            }
        }

        // Cubeta (posición) donde se almacena o se almacenaría el código.
        public int IndiceCubeta(string codigo)
        {
            return FuncionHash(codigo ?? "");
        }

        // Paradas de una cubeta, en el orden de su cadena.
        public Parada[] ObtenerCubeta(int indice)
        {
            if (indice < 0 || indice >= tabla.Length) return new Parada[0];

            int total = 0;
            for (Nodo n = tabla[indice]; n != null; n = n.Siguiente) total++;

            Parada[] resultado = new Parada[total];
            int k = 0;
            for (Nodo n = tabla[indice]; n != null; n = n.Siguiente) resultado[k++] = n.Dato;
            return resultado;
        }

        // Todas las paradas almacenadas (recorrido de la tabla).
        public Parada[] ObtenerTodas()
        {
            Parada[] resultado = new Parada[cantidad];
            int k = 0;
            for (int i = 0; i < tabla.Length; i++)
                for (Nodo n = tabla[i]; n != null && k < resultado.Length; n = n.Siguiente)
                    resultado[k++] = n.Dato;
            return resultado;
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