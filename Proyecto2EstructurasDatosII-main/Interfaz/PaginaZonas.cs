using System;
using System.Drawing;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Models;
using Proyecto2EstructurasDatosII.Servicios;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Zonas conectadas (Union-Find) y verificación de conectividad entre dos paradas.
    public class PaginaZonas : PaginaBase
    {
        private readonly CajaAutocompletar cajaA, cajaB;
        private readonly Label lblResultado;
        private readonly Panel panelResultado;
        private readonly TreeView arbol;
        private readonly MapaControl mapa;

        public PaginaZonas(Contexto ctx)
            : base(ctx, "Zonas conectadas", "El Union-Find agrupa las paradas que se pueden alcanzar entre sí a través de rutas.")
        {
            TableLayoutPanel cols = Columnas(372);
            const int W = 326;

            TarjetaPanel izq = new TarjetaPanel("Verificar conectividad");
            TableLayoutPanel interior = new TableLayoutPanel();
            interior.Dock = DockStyle.Fill;
            interior.ColumnCount = 1;
            interior.RowCount = 2;
            interior.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            interior.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            interior.BackColor = Tema.Tarjeta;

            FlowLayoutPanel pila = new FlowLayoutPanel();
            pila.FlowDirection = FlowDirection.TopDown;
            pila.WrapContents = false;
            pila.AutoSize = true;
            pila.Dock = DockStyle.Top;
            pila.BackColor = Tema.Tarjeta;
            pila.Margin = Padding.Empty;

            pila.Controls.Add(Tema.Campo("Primera parada"));
            cajaA = new CajaAutocompletar();
            cajaA.Width = W;
            cajaA.Enlazar(ctx.Red);
            cajaA.PistaTexto = "Escriba nombre o código…";
            pila.Controls.Add(cajaA);

            pila.Controls.Add(Tema.Campo("Segunda parada"));
            cajaB = new CajaAutocompletar();
            cajaB.Width = W;
            cajaB.Enlazar(ctx.Red);
            cajaB.PistaTexto = "Escriba nombre o código…";
            pila.Controls.Add(cajaB);

            Button verificar = Tema.Boton("Verificar conectividad", true);
            verificar.Width = W;
            verificar.Margin = new Padding(0, 10, 0, 0);
            verificar.Click += delegate { Verificar(); };
            pila.Controls.Add(verificar);

            panelResultado = new Panel();
            panelResultado.Width = W;
            panelResultado.Height = 64;
            panelResultado.Margin = new Padding(0, 12, 0, 12);
            panelResultado.BackColor = Tema.Fondo;
            lblResultado = Tema.Etiqueta("Elija dos paradas y pulse «Verificar».", 9.5f, false, Tema.TextoSuave);
            lblResultado.Location = new Point(14, 10);
            lblResultado.AutoSize = false;
            lblResultado.Size = new Size(W - 28, 46);
            panelResultado.Controls.Add(lblResultado);
            pila.Controls.Add(panelResultado);

            Label tz = Tema.Titulo("Zonas de la red");
            tz.Margin = new Padding(0, 4, 0, 6);
            pila.Controls.Add(tz);

            arbol = new TreeView();
            arbol.Dock = DockStyle.Fill;
            arbol.Margin = Padding.Empty;
            arbol.BorderStyle = BorderStyle.None;
            arbol.BackColor = Tema.Fondo;
            arbol.Font = Tema.Fuente(9.5f);
            arbol.ShowLines = false;
            arbol.ShowPlusMinus = true;
            arbol.ItemHeight = 26;
            arbol.HideSelection = false;

            interior.Controls.Add(pila, 0, 0);
            interior.Controls.Add(arbol, 0, 1);
            izq.Contenido.Controls.Add(interior);

            TarjetaPanel der = new TarjetaPanel("Mapa coloreado por zona");
            der.Contenido.Padding = new Padding(0);
            mapa = new MapaControl();
            mapa.Dock = DockStyle.Fill;
            mapa.Enlazar(ctx.Red);
            mapa.ColorearZonas = true;
            der.Contenido.Controls.Add(mapa);

            cols.Controls.Add(izq, 0, 0);
            cols.Controls.Add(der, 2, 0);
            Cuerpo.Controls.Add(cols);
        }

        public override void Actualizar()
        {
            mapa.Recargar();

            arbol.BeginUpdate();
            arbol.Nodes.Clear();
            Zona[] zonas = Ctx.Red.ObtenerZonas();
            for (int z = 0; z < zonas.Length; z++)
            {
                TreeNode nodo = arbol.Nodes.Add("Zona " + zonas[z].Numero + "  ·  " + zonas[z].Paradas.Length +
                                                (zonas[z].Paradas.Length == 1 ? " parada" : " paradas"));
                nodo.ForeColor = Tema.ColorZona(z);
                nodo.NodeFont = Tema.FuenteNegrita(9.5f);
                for (int k = 0; k < zonas[z].Paradas.Length; k++)
                {
                    TreeNode hijo = nodo.Nodes.Add(zonas[z].Paradas[k].Codigo + "  " + zonas[z].Paradas[k].Nombre);
                    hijo.ForeColor = Tema.Texto;
                }
                nodo.Expand();
            }
            arbol.EndUpdate();
        }

        private void Verificar()
        {
            Parada a = cajaA.ObtenerParada();
            Parada b = cajaB.ObtenerParada();
            if (a == null || b == null)
            {
                Mostrar("Elija dos paradas que existan en la red.", ColorTranslator.FromHtml("#FDECEC"), Tema.Peligro);
                return;
            }

            int ra = Ctx.Red.Zonas.Buscar(a.Indice);
            int rb = Ctx.Red.Zonas.Buscar(b.Indice);
            if (ra == rb)
            {
                Mostrar("✔  " + a.Codigo + " y " + b.Codigo + " están en la misma zona conectada (misma raíz del Union-Find: " + ra + ").",
                        ColorTranslator.FromHtml("#E8F7EE"), Tema.Exito);
                mapa.Camino = null;
            }
            else
            {
                Mostrar("✖  " + a.Codigo + " y " + b.Codigo + " están en zonas distintas (raíces " + ra + " y " + rb + "). " +
                        "No existe ninguna ruta entre ellas.", ColorTranslator.FromHtml("#FDECEC"), Tema.Peligro);
            }
            mapa.ParadaMarcada = a.Codigo;
        }

        private void Mostrar(string texto, Color fondo, Color letra)
        {
            lblResultado.Text = texto;
            lblResultado.ForeColor = letra;
            lblResultado.BackColor = fondo;
            panelResultado.BackColor = fondo;
        }
    }
}
