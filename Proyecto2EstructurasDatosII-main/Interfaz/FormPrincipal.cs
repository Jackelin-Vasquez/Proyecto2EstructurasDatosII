using System;
using System.Drawing;
using System.Windows.Forms;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Botón de navegación de la barra lateral.
    public class BotonNavegacion : Control
    {
        private bool hover;
        private bool activo;
        public string Icono { get; set; }

        public BotonNavegacion(string icono, string texto)
        {
            Icono = icono;
            Text = texto;
            Height = 46;
            Cursor = Cursors.Hand;
            Font = Tema.Fuente(10.5f);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public bool Activo
        {
            get { return activo; }
            set { activo = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Color fondo = activo ? Tema.LateralActivo : (hover ? Tema.LateralHover : Tema.Lateral);
            g.Clear(fondo);

            if (activo)
            {
                using (Brush b = new SolidBrush(ColorTranslator.FromHtml("#5B9BFF")))
                    g.FillRectangle(b, 0, 0, 4, Height);
            }

            Color texto = activo ? Color.White : Tema.LateralTexto;
            using (Font fi = new Font("Segoe UI Symbol", 12f, FontStyle.Regular, GraphicsUnit.Point))
            using (Brush bt = new SolidBrush(texto))
            using (Font ft = activo ? Tema.FuenteNegrita(10.5f) : Tema.Fuente(10.5f))
            {
                StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(Icono, fi, bt, new RectangleF(22, 0, 34, Height), sf);
                g.DrawString(Text, ft, bt, new RectangleF(58, 0, Width - 60, Height), sf);
            }
        }
    }

    public class FormPrincipal : Form
    {
        private readonly Contexto ctx = new Contexto();
        private readonly PaginaBase[] paginas;
        private readonly BotonNavegacion[] botones;
        private readonly Panel contenedor;
        private readonly Label lblEstado, lblResumen;
        private readonly Panel barraEstado;
        private int paginaActual = -1;

        public FormPrincipal()
        {
            Text = "Red de Transporte Urbano · Estructuras de Datos II";
            Font = Tema.Fuente(9f);
            BackColor = Tema.Fondo;
            ClientSize = new Size(1380, 840);
            MinimumSize = new Size(1180, 720);
            StartPosition = FormStartPosition.CenterScreen;

            // ---------- Barra de estado ----------
            barraEstado = new Panel();
            barraEstado.Dock = DockStyle.Bottom;
            barraEstado.Height = 32;
            barraEstado.BackColor = Color.White;
            barraEstado.Paint += delegate (object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borde)) e.Graphics.DrawLine(p, 0, 0, barraEstado.Width, 0);
            };
            lblEstado = Tema.Etiqueta("Listo.", 9f, false, Tema.TextoSuave);
            lblEstado.Location = new Point(18, 8);
            lblResumen = Tema.Etiqueta("", 9f, false, Tema.TextoSuave);
            lblResumen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblResumen.Location = new Point(ClientSize.Width - 420, 8);
            barraEstado.Controls.Add(lblEstado);
            barraEstado.Controls.Add(lblResumen);

            // ---------- Contenedor de páginas ----------
            contenedor = new Panel();
            contenedor.Dock = DockStyle.Fill;
            contenedor.BackColor = Tema.Fondo;

            paginas = new PaginaBase[]
            {
                new PaginaMapa(ctx),
                new PaginaParadas(ctx),
                new PaginaRutas(ctx),
                new PaginaCalculo(ctx),
                new PaginaRecorridos(ctx),
                new PaginaZonas(ctx),
                new PaginaEstructuras(ctx),
                new PaginaDatos(ctx)
            };
            for (int i = 0; i < paginas.Length; i++)
            {
                paginas[i].Visible = false;
                contenedor.Controls.Add(paginas[i]);
            }

            // ---------- Barra lateral ----------
            Panel lateral = new Panel();
            lateral.Dock = DockStyle.Left;
            lateral.Width = 250;
            lateral.BackColor = Tema.Lateral;

            Panel marca = new Panel();
            marca.Dock = DockStyle.Top;
            marca.Height = 104;
            marca.BackColor = Tema.Lateral;
            marca.Paint += delegate (object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Brush b = new SolidBrush(Tema.Primario))
                    g.FillEllipse(b, 22, 28, 44, 44);
                using (Pen p = new Pen(Color.White, 2.5f))
                {
                    g.DrawLine(p, 36, 56, 44, 44);
                    g.DrawLine(p, 44, 44, 54, 52);
                }
                using (Brush w = new SolidBrush(Color.White))
                {
                    g.FillEllipse(w, 32, 52, 8, 8);
                    g.FillEllipse(w, 40, 40, 8, 8);
                    g.FillEllipse(w, 50, 48, 8, 8);
                }
            };
            Label t1 = Tema.Etiqueta("Red de Transporte", 12f, true, Color.White);
            t1.Location = new Point(76, 30);
            Label t2 = Tema.Etiqueta("Estructuras de Datos II", 8.5f, false, Tema.LateralTexto);
            t2.Location = new Point(77, 54);
            marca.Controls.Add(t1);
            marca.Controls.Add(t2);

            string[] iconos = { "⌂", "⚑", "⇄", "➜", "⟳", "◍", "▤", "⇩" };
            string[] nombres =
            {
                "Mapa de la red", "Paradas", "Rutas", "Buscar ruta",
                "Recorridos", "Zonas conectadas", "Estructuras", "Datos y archivos"
            };
            botones = new BotonNavegacion[nombres.Length];

            Panel nav = new Panel();
            nav.Dock = DockStyle.Fill;
            nav.BackColor = Tema.Lateral;
            for (int i = 0; i < nombres.Length; i++)
            {
                BotonNavegacion b = new BotonNavegacion(iconos[i], nombres[i]);
                b.Location = new Point(0, 8 + i * 48);
                b.Width = lateral.Width;
                b.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                int destino = i;
                b.Click += delegate { Mostrar(destino); };
                botones[i] = b;
                nav.Controls.Add(b);
            }

            Label pie = Tema.Etiqueta("Tabla Hash · Grafo · Union-Find · Trie", 8f, false, Color.FromArgb(120, 140, 175));
            pie.Dock = DockStyle.Bottom;
            pie.AutoSize = false;
            pie.Height = 38;
            pie.TextAlign = ContentAlignment.MiddleCenter;

            lateral.Controls.Add(nav);
            lateral.Controls.Add(pie);
            lateral.Controls.Add(marca);

            // Orden de adición: Fill primero, luego los bordes.
            Controls.Add(contenedor);
            Controls.Add(barraEstado);
            Controls.Add(lateral);

            ctx.DatosCambiados += AlCambiarDatos;
            ctx.Mensaje += MostrarMensaje;

            Load += delegate
            {
                // Datos de ejemplo al abrir (si existe la carpeta Recursos).
                ((PaginaDatos)paginas[7]).CargarEjemplo(true);
                Mostrar(0);
            };
        }

        private void Mostrar(int indice)
        {
            if (indice == paginaActual) { paginas[indice].AlMostrar(); return; }

            for (int i = 0; i < paginas.Length; i++)
            {
                paginas[i].Visible = i == indice;
                botones[i].Activo = i == indice;
            }
            paginaActual = indice;
            paginas[indice].AlMostrar();
            ActualizarResumen();
        }

        private void AlCambiarDatos()
        {
            if (paginaActual >= 0) paginas[paginaActual].Actualizar();
            ActualizarResumen();
        }

        private void ActualizarResumen()
        {
            lblResumen.Text = "Paradas: " + ctx.Red.CantidadParadas + "   ·   Rutas: " + ctx.Red.CantidadRutas +
                              "   ·   Zonas: " + ctx.Red.Zonas.Componentes;
        }

        private void MostrarMensaje(string texto, TipoMensaje tipo)
        {
            lblEstado.Text = (tipo == TipoMensaje.Exito ? "✔  " : tipo == TipoMensaje.Error ? "✖  " : "ℹ  ") + texto;
            lblEstado.ForeColor = tipo == TipoMensaje.Exito ? Tema.Exito :
                                  tipo == TipoMensaje.Error ? Tema.Peligro : Tema.TextoSuave;
        }
    }
}
