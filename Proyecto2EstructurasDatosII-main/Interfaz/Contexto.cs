using System;
using Proyecto2EstructurasDatosII.Servicios;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    public enum TipoMensaje
    {
        Info,
        Exito,
        Error
    }

    // Integrantes responsables de cada estructura (se muestran en la pestaña «Estructuras»).
    // Reemplace los textos entre corchetes por los nombres reales del grupo.
    public static class Responsables
    {
        public const string TablaHash = "Morelia";
        public const string Grafo = "[Nombre del integrante]";
        public const string UnionFind = "[Nombre del integrante]";
        public const string Trie = "[Nombre del integrante]";
        public const string Modelos = "Morelia / Jackelin Vásquez";
    }

    // Estado compartido por todas las páginas: la red y los avisos de cambio.
    public class Contexto
    {
        public RedTransporte Red { get; private set; }

        // Se dispara cuando cambian los datos (paradas, rutas, carga de archivos...).
        public event Action DatosCambiados;

        // Mensaje para la barra de estado.
        public event Action<string, TipoMensaje> Mensaje;

        public Contexto()
        {
            Red = new RedTransporte();
        }

        public void NotificarCambio()
        {
            if (DatosCambiados != null) DatosCambiados();
        }

        public void Avisar(string texto, TipoMensaje tipo)
        {
            if (Mensaje != null) Mensaje(texto, tipo);
        }
    }
}
