// Interfaz gráfica (Windows Forms): colores, fuentes y fábricas de controles con estilo.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    public static class Tema
    {
        // Paleta
        public static readonly Color Lateral = ColorTranslator.FromHtml("#14213D");
        public static readonly Color LateralHover = ColorTranslator.FromHtml("#1D2F55");
        public static readonly Color LateralActivo = ColorTranslator.FromHtml("#243A68");
        public static readonly Color LateralTexto = ColorTranslator.FromHtml("#C9D3E6");
        public static readonly Color Fondo = ColorTranslator.FromHtml("#F3F5F9");
        public static readonly Color Tarjeta = Color.White;
        public static readonly Color Borde = ColorTranslator.FromHtml("#E1E6EF");
        public static readonly Color Texto = ColorTranslator.FromHtml("#1B2433");
        public static readonly Color TextoSuave = ColorTranslator.FromHtml("#667085");
        public static readonly Color Primario = ColorTranslator.FromHtml("#2563EB");
        public static readonly Color PrimarioOscuro = ColorTranslator.FromHtml("#1D4ED8");
        public static readonly Color Exito = ColorTranslator.FromHtml("#16A34A");
        public static readonly Color Alerta = ColorTranslator.FromHtml("#D97706");
        public static readonly Color Peligro = ColorTranslator.FromHtml("#DC2626");
        public static readonly Color FilaAlterna = ColorTranslator.FromHtml("#F8FAFD");
        public static readonly Color Seleccion = ColorTranslator.FromHtml("#DBE7FF");

        // Colores categóricos para las zonas conectadas.
        public static readonly Color[] Zonas = new Color[]
        {
            ColorTranslator.FromHtml("#2563EB"), ColorTranslator.FromHtml("#F97316"),
            ColorTranslator.FromHtml("#10B981"), ColorTranslator.FromHtml("#A855F7"),
            ColorTranslator.FromHtml("#EF4444"), ColorTranslator.FromHtml("#0EA5E9"),
            ColorTranslator.FromHtml("#EAB308"), ColorTranslator.FromHtml("#EC4899")
        };

        public static Color ColorZona(int indice)
        {
            return Zonas[((indice % Zonas.Length) + Zonas.Length) % Zonas.Length];
        }

        private const string Familia = "Segoe UI";

        public static Font Fuente(float tamano)
        {
            return new Font(Familia, tamano, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Font FuenteNegrita(float tamano)
        {
            return new Font(Familia, tamano, FontStyle.Bold, GraphicsUnit.Point);
        }

        public static Font FuenteMono(float tamano)
        {
            return new Font("Consolas", tamano, FontStyle.Regular, GraphicsUnit.Point);
        }

        // ---------------------------------------------------------------
        //  Fábricas de controles
        // ---------------------------------------------------------------

        public static Label Etiqueta(string texto, float tamano, bool negrita, Color color)
        {
            Label l = new Label();
            l.Text = texto;
            l.AutoSize = true;
            l.Font = negrita ? FuenteNegrita(tamano) : Fuente(tamano);
            l.ForeColor = color;
            l.BackColor = Color.Transparent;
            return l;
        }

        public static Label Titulo(string texto)
        {
            return Etiqueta(texto, 11.5f, true, Texto);
        }

        public static Label Campo(string texto)
        {
            Label l = Etiqueta(texto, 9f, false, TextoSuave);
            l.Margin = new Padding(0, 10, 0, 2);
            return l;
        }

        public static Button Boton(string texto, bool primario)
        {
            Button b = new Button();
            b.Text = texto;
            b.Height = 36;
            b.Margin = new Padding(0, 3, 0, 3);
            b.AutoSize = false;
            b.Cursor = Cursors.Hand;
            b.FlatStyle = FlatStyle.Flat;
            b.Font = FuenteNegrita(9.5f);
            b.UseVisualStyleBackColor = false;
            if (primario)
            {
                b.BackColor = Primario;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = PrimarioOscuro;
                b.FlatAppearance.MouseDownBackColor = PrimarioOscuro;
            }
            else
            {
                b.BackColor = Color.White;
                b.ForeColor = Texto;
                b.FlatAppearance.BorderColor = Borde;
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.MouseOverBackColor = Fondo;
                b.FlatAppearance.MouseDownBackColor = Borde;
            }
            return b;
        }

        public static Button BotonPeligro(string texto)
        {
            Button b = Boton(texto, false);
            b.ForeColor = Peligro;
            b.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#F4B5B5");
            b.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#FDECEC");
            return b;
        }

        public static TextBox Entrada()
        {
            TextBox t = new TextBox();
            t.Font = Fuente(10f);
            t.BorderStyle = BorderStyle.FixedSingle;
            t.BackColor = Color.White;
            t.ForeColor = Texto;
            t.Margin = new Padding(0, 0, 0, 2);
            return t;
        }

        // Contenedor blanco con borde fino (la "tarjeta" de la interfaz).
        public static Panel NuevaTarjeta()
        {
            Panel p = new Panel();
            p.BackColor = Tarjeta;
            p.Padding = new Padding(18);
            p.Paint += delegate (object s, PaintEventArgs e)
            {
                Panel q = (Panel)s;
                using (Pen pen = new Pen(Borde))
                    e.Graphics.DrawRectangle(pen, 0, 0, q.Width - 1, q.Height - 1);
            };
            return p;
        }

        public static void EstilizarTabla(DataGridView g)
        {
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Borde;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersDefaultCellStyle.BackColor = Fondo;
            g.ColumnHeadersDefaultCellStyle.ForeColor = TextoSuave;
            g.ColumnHeadersDefaultCellStyle.Font = FuenteNegrita(9f);
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Fondo;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextoSuave;
            g.ColumnHeadersHeight = 36;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.DefaultCellStyle.Font = Fuente(9.5f);
            g.DefaultCellStyle.ForeColor = Texto;
            g.DefaultCellStyle.BackColor = Color.White;
            g.DefaultCellStyle.SelectionBackColor = Seleccion;
            g.DefaultCellStyle.SelectionForeColor = Texto;
            g.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = FilaAlterna;
            g.RowTemplate.Height = 32;
            g.RowHeadersVisible = false;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.ReadOnly = true;
            g.MultiSelect = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        public static void EstilizarLista(ListBox l)
        {
            l.BorderStyle = BorderStyle.None;
            l.Font = Fuente(9.5f);
            l.ForeColor = Texto;
            l.BackColor = Color.White;
            l.IntegralHeight = false;
        }
    }
}
