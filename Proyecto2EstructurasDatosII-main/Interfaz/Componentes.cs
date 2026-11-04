// Componentes reutilizables de la interfaz: tarjeta con título, tarjeta de dato y página base.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Tarjeta blanca con encabezado (título + acciones a la derecha) y contenido.
    public class TarjetaPanel : Panel
    {
        public Panel Contenido { get; private set; }
        public FlowLayoutPanel Acciones { get; private set; }
        private readonly Label titulo;
        private readonly Panel encabezado;

        public TarjetaPanel(string textoTitulo)
        {
            BackColor = Tema.Tarjeta;
            Padding = new Padding(1);
            Dock = DockStyle.Fill;

            Contenido = new Panel();
            Contenido.Dock = DockStyle.Fill;
            Contenido.Padding = new Padding(18, 14, 18, 18);
            Contenido.BackColor = Tema.Tarjeta;

            encabezado = new Panel();
            encabezado.Dock = DockStyle.Top;
            encabezado.Height = 50;
            encabezado.BackColor = Tema.Tarjeta;
            encabezado.Paint += delegate (object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borde))
                    e.Graphics.DrawLine(p, 0, encabezado.Height - 1, encabezado.Width, encabezado.Height - 1);
            };

            titulo = Tema.Titulo(textoTitulo);
            titulo.Location = new Point(18, 15);
            encabezado.Controls.Add(titulo);

            Acciones = new FlowLayoutPanel();
            Acciones.Dock = DockStyle.Right;
            Acciones.AutoSize = true;
            Acciones.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Acciones.WrapContents = false;
            Acciones.FlowDirection = FlowDirection.LeftToRight;
            Acciones.Padding = new Padding(0, 10, 12, 0);
            Acciones.BackColor = Tema.Tarjeta;
            encabezado.Controls.Add(Acciones);

            // El contenido se agrega primero (queda detrás) y el encabezado después.
            Controls.Add(Contenido);
            Controls.Add(encabezado);
        }

        public string Titulo
        {
            get { return titulo.Text; }
            set { titulo.Text = value; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Tema.Borde))
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }
    }

    // Indicador numérico grande (paradas, rutas, zonas...).
    public class TarjetaDato : Panel
    {
        private readonly Label etiqueta;
        private readonly Label valor;
        private readonly Label detalle;
        private readonly Color acento;

        public TarjetaDato(string titulo, Color color)
        {
            acento = color;
            BackColor = Tema.Tarjeta;
            Dock = DockStyle.Fill;
            Margin = new Padding(0, 0, 14, 0);
            Padding = new Padding(20, 14, 12, 10);

            etiqueta = Tema.Etiqueta(titulo.ToUpperInvariant(), 8.5f, true, Tema.TextoSuave);
            etiqueta.Location = new Point(20, 14);

            valor = Tema.Etiqueta("0", 22f, true, Tema.Texto);
            valor.Location = new Point(18, 32);

            detalle = Tema.Etiqueta("", 8.5f, false, Tema.TextoSuave);
            detalle.Location = new Point(20, 72);

            Controls.Add(etiqueta);
            Controls.Add(valor);
            Controls.Add(detalle);
        }

        public void Establecer(string textoValor, string textoDetalle)
        {
            valor.Text = textoValor;
            detalle.Text = textoDetalle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Tema.Borde))
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            using (Brush b = new SolidBrush(acento))
                e.Graphics.FillRectangle(b, 0, 0, 5, Height);
        }
    }

    // Página con encabezado (título y subtítulo) y un cuerpo con margen.
    public abstract class PaginaBase : UserControl
    {
        protected Contexto Ctx { get; private set; }
        protected Panel Cuerpo { get; private set; }

        protected PaginaBase(Contexto ctx, string titulo, string subtitulo)
        {
            Ctx = ctx;
            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;
            DoubleBuffered = true;

            Cuerpo = new Panel();
            Cuerpo.Dock = DockStyle.Fill;
            Cuerpo.Padding = new Padding(28, 4, 28, 22);
            Cuerpo.BackColor = Tema.Fondo;

            Panel cabecera = new Panel();
            cabecera.Dock = DockStyle.Top;
            cabecera.Height = 84;
            cabecera.BackColor = Tema.Fondo;

            Label t = Tema.Etiqueta(titulo, 18f, true, Tema.Texto);
            t.Location = new Point(28, 16);
            Label s = Tema.Etiqueta(subtitulo, 9.5f, false, Tema.TextoSuave);
            s.Location = new Point(30, 52);
            cabecera.Controls.Add(t);
            cabecera.Controls.Add(s);

            Controls.Add(Cuerpo);
            Controls.Add(cabecera);
        }

        // Cada página vuelve a leer la red cuando cambian los datos.
        public virtual void Actualizar() { }

        // Se llama al mostrar la página.
        public virtual void AlMostrar() { Actualizar(); }

        protected static TableLayoutPanel Columnas(float anchoIzquierda)
        {
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 3;
            t.RowCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, anchoIzquierda));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            t.BackColor = Tema.Fondo;
            return t;
        }

        // Pila vertical de controles de ancho fijo (formularios).
        protected static FlowLayoutPanel Pila()
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.Dock = DockStyle.Fill;
            f.FlowDirection = FlowDirection.TopDown;
            f.WrapContents = false;
            f.AutoScroll = false;
            f.HorizontalScroll.Enabled = false;
            f.HorizontalScroll.Visible = false;
            f.HorizontalScroll.Maximum = 0;
            f.AutoScroll = true;      // solo desplazamiento vertical
            f.BackColor = Color.Transparent;
            return f;
        }
    }
}
