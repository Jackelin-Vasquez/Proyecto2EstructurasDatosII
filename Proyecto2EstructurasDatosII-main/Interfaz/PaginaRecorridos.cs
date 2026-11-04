using System;
using System.Drawing;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Models;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Recorridos del grafo: BFS (anchura) y DFS (profundidad) desde una parada.
    public class PaginaRecorridos : PaginaBase
    {
        private readonly CajaAutocompletar cajaInicio;
        private readonly Label lblTitulo, lblExplicacion;
        private readonly ListView lista;
        private readonly MapaControl mapa;

        public PaginaRecorridos(Contexto ctx)
            : base(ctx, "Recorridos del grafo", "Explore la red desde una parada con búsqueda en anchura (BFS) o en profundidad (DFS).")
        {
            TableLayoutPanel cols = Columnas(372);
            const int W = 326;

            TarjetaPanel izq = new TarjetaPanel("Recorrer la red");
            FlowLayoutPanel pila = Pila();
            izq.Contenido.Controls.Add(pila);

            pila.Controls.Add(Tema.Campo("Parada de inicio"));
            cajaInicio = new CajaAutocompletar();
            cajaInicio.Width = W;
            cajaInicio.Enlazar(ctx.Red);
            cajaInicio.PistaTexto = "Escriba nombre o código…";
            pila.Controls.Add(cajaInicio);

            TableLayoutPanel botones = new TableLayoutPanel();
            botones.Width = W;
            botones.Height = 44;
            botones.ColumnCount = 2;
            botones.RowCount = 1;
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            botones.Margin = new Padding(0, 8, 0, 0);
            Button bfs = Tema.Boton("Recorrido BFS", true);
            bfs.Dock = DockStyle.Fill;
            bfs.Margin = new Padding(0, 0, 6, 4);
            bfs.Click += delegate { Recorrer(true); };
            Button dfs = Tema.Boton("Recorrido DFS", true);
            dfs.Dock = DockStyle.Fill;
            dfs.Margin = new Padding(6, 0, 0, 4);
            dfs.Click += delegate { Recorrer(false); };
            botones.Controls.Add(bfs, 0, 0);
            botones.Controls.Add(dfs, 1, 0);
            pila.Controls.Add(botones);

            lblTitulo = Tema.Etiqueta("Orden de visita", 10.5f, true, Tema.Texto);
            lblTitulo.Margin = new Padding(0, 16, 0, 2);
            pila.Controls.Add(lblTitulo);

            lblExplicacion = Tema.Etiqueta("Elija una parada y pulse BFS o DFS. Los números en el mapa indican el orden de visita.",
                                           9f, false, Tema.TextoSuave);
            lblExplicacion.AutoSize = false;
            lblExplicacion.Size = new Size(W, 34);
            lblExplicacion.Margin = new Padding(0, 0, 0, 8);
            pila.Controls.Add(lblExplicacion);

            lista = new ListView();
            lista.View = View.Details;
            lista.FullRowSelect = true;
            lista.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lista.BorderStyle = BorderStyle.None;
            lista.BackColor = Tema.Fondo;
            lista.Font = Tema.Fuente(9.5f);
            lista.Width = W;
            lista.Height = 300;
            lista.Margin = new Padding(0);
            lista.Columns.Add("#", 40);
            lista.Columns.Add("Código", 66);
            lista.Columns.Add("Parada", 200);
            pila.Controls.Add(lista);

            TarjetaPanel der = new TarjetaPanel("Orden de visita en el mapa");
            der.Contenido.Padding = new Padding(0);
            mapa = new MapaControl();
            mapa.Dock = DockStyle.Fill;
            mapa.Enlazar(ctx.Red);
            mapa.ColorearZonas = false;
            mapa.MostrarLeyenda = false;
            mapa.MostrarEtiquetasRutas = false;
            der.Contenido.Controls.Add(mapa);

            cols.Controls.Add(izq, 0, 0);
            cols.Controls.Add(der, 2, 0);
            Cuerpo.Controls.Add(cols);
        }

        public override void Actualizar()
        {
            mapa.Recargar();
            if (cajaInicio.ObtenerParada() == null)
            {
                lista.Items.Clear();
                mapa.OrdenVisita = null;
            }
        }

        private void Recorrer(bool anchura)
        {
            Parada inicio = cajaInicio.ObtenerParada();
            if (inicio == null)
            {
                Ctx.Avisar("Elija una parada de inicio que exista en la red.", TipoMensaje.Error);
                MessageBox.Show("Elija una parada de inicio que exista en la red.", "Recorrido",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string[] orden = anchura ? Ctx.Red.Grafo.RecorridoBFS(inicio.Codigo)
                                     : Ctx.Red.Grafo.RecorridoDFS(inicio.Codigo);

            lista.BeginUpdate();
            lista.Items.Clear();
            for (int i = 0; i < orden.Length; i++)
            {
                Parada p = Ctx.Red.Localizar(orden[i]);
                ListViewItem item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(orden[i]);
                item.SubItems.Add(p == null ? "" : p.Nombre);
                lista.Items.Add(item);
            }
            lista.EndUpdate();

            string tipo = anchura ? "BFS" : "DFS";
            lblTitulo.Text = "Orden de visita · " + tipo;
            lblExplicacion.Text = (anchura
                ? "BFS visita primero las paradas más cercanas (por número de tramos), usando una cola. "
                : "DFS avanza lo más profundo posible por cada ramal antes de retroceder. ") +
                orden.Length + " de " + Ctx.Red.CantidadParadas + " paradas alcanzadas siguiendo el sentido de las rutas.";

            mapa.OrdenVisita = orden;
            mapa.ParadaMarcada = inicio.Codigo;
            Ctx.Avisar("Recorrido " + tipo + " desde " + inicio.Codigo + ": " + orden.Length + " paradas.", TipoMensaje.Exito);
        }
    }
}
