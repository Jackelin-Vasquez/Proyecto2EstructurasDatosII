// Caja de texto con autocompletado: el Trie sugiere mientras se escribe y la Tabla Hash
// entrega el registro completo de la parada elegida.
using System;
using System.Drawing;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Models;
using Proyecto2EstructurasDatosII.Servicios;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    public class CajaAutocompletar : UserControl
    {
        private readonly TextBox caja;
        private readonly Label pista;
        private readonly ListBox lista;
        private readonly ToolStripDropDown popup;
        private readonly ToolStripControlHost host;
        private RedTransporte red;
        private Parada[] sugerencias = new Parada[0];
        private Parada elegida;
        private bool suprimir;

        // Se dispara al elegir una sugerencia o pulsar Enter con un texto válido.
        public event Action<Parada> Elegida;

        // Se dispara cada vez que cambia el texto.
        public event EventHandler TextoModificado;

        public CajaAutocompletar()
        {
            Height = 36;
            BackColor = Color.White;
            Margin = new Padding(0, 0, 0, 4);

            caja = new TextBox();
            caja.BorderStyle = BorderStyle.None;
            caja.Font = Tema.Fuente(10f);
            caja.ForeColor = Tema.Texto;
            caja.BackColor = Color.White;
            caja.Location = new Point(10, 8);
            caja.Width = Width - 20;
            caja.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            caja.TextChanged += CajaTextoCambiado;
            caja.KeyDown += CajaTeclaPresionada;
            caja.Leave += delegate { BeginInvoke(new Action(OcultarSiPerdioFoco)); };
            caja.Enter += delegate { Invalidate(); };
            caja.GotFocus += delegate { Invalidate(); };
            caja.LostFocus += delegate { Invalidate(); };

            pista = new Label();
            pista.AutoSize = true;
            pista.ForeColor = Color.FromArgb(160, 168, 182);
            pista.BackColor = Color.White;
            pista.Font = Tema.Fuente(10f);
            pista.Location = new Point(10, 8);
            pista.Click += delegate { caja.Focus(); };

            Controls.Add(caja);
            Controls.Add(pista);
            pista.BringToFront();

            lista = new ListBox();
            lista.BorderStyle = BorderStyle.None;
            lista.DrawMode = DrawMode.OwnerDrawFixed;
            lista.ItemHeight = 42;
            lista.IntegralHeight = false;
            lista.TabStop = false;
            lista.DrawItem += DibujarSugerencia;
            lista.MouseUp += delegate (object s, MouseEventArgs e)
            {
                int i = lista.IndexFromPoint(e.Location);
                if (i >= 0 && i < sugerencias.Length) Elegir(sugerencias[i]);
            };
            lista.Leave += delegate { BeginInvoke(new Action(OcultarSiPerdioFoco)); };

            host = new ToolStripControlHost(lista);
            host.Margin = Padding.Empty;
            host.Padding = Padding.Empty;
            host.AutoSize = false;

            popup = new ToolStripDropDown();
            popup.AutoClose = false;
            popup.Padding = Padding.Empty;
            popup.Margin = Padding.Empty;
            popup.Items.Add(host);
        }

        public void Enlazar(RedTransporte r)
        {
            red = r;
        }

        public string PistaTexto
        {
            get { return pista.Text; }
            set { pista.Text = value; ActualizarPista(); }
        }

        public string Texto
        {
            get { return caja.Text; }
            set { suprimir = true; caja.Text = value; suprimir = false; elegida = null; ActualizarPista(); }
        }

        // Parada que corresponde a lo escrito (por selección, por código o por nombre exacto).
        public Parada ObtenerParada()
        {
            if (elegida != null && red != null && red.Localizar(elegida.Codigo) == elegida) return elegida;
            return red == null ? null : red.ResolverParada(caja.Text);
        }

        // true si la parada se eligió de la lista de sugerencias del Trie.
        public bool EligioSugerencia
        {
            get { return elegida != null; }
        }

        public void Establecer(Parada p)
        {
            suprimir = true;
            caja.Text = p == null ? "" : p.Nombre;
            suprimir = false;
            elegida = p;
            OcultarPopup();
            ActualizarPista();
            Invalidate();
        }

        public void Limpiar()
        {
            Establecer(null);
        }

        private void ActualizarPista()
        {
            pista.Visible = caja.Text.Length == 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Color c = caja.Focused ? Tema.Primario : Color.FromArgb(205, 212, 225);
            using (Pen p = new Pen(c, caja.Focused ? 2f : 1f))
            {
                float d = caja.Focused ? 1f : 0f;
                e.Graphics.DrawRectangle(p, d, d, Width - 1 - 2 * d, Height - 1 - 2 * d);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        // -------------------------------------------------------------
        //  Sugerencias
        // -------------------------------------------------------------

        private void CajaTextoCambiado(object sender, EventArgs e)
        {
            ActualizarPista();
            if (suprimir) return;
            elegida = null;

            if (red != null)
            {
                sugerencias = red.Sugerir(caja.Text, 8);
                if (sugerencias.Length > 0 && caja.Focused) MostrarPopup();
                else OcultarPopup();
            }

            if (TextoModificado != null) TextoModificado(this, EventArgs.Empty);
        }

        private void MostrarPopup()
        {
            lista.BeginUpdate();
            lista.Items.Clear();
            for (int i = 0; i < sugerencias.Length; i++) lista.Items.Add(sugerencias[i]);
            lista.SelectedIndex = 0;
            lista.EndUpdate();

            int alto = sugerencias.Length * lista.ItemHeight + 2;
            lista.Size = new Size(Width, alto);
            host.Size = lista.Size;
            popup.Size = new Size(Width, alto);
            popup.Show(this, new Point(0, Height));
        }

        private void OcultarPopup()
        {
            if (popup.Visible) popup.Close();
        }

        private void OcultarSiPerdioFoco()
        {
            if (!caja.Focused && !lista.Focused) OcultarPopup();
        }

        private void Elegir(Parada p)
        {
            Establecer(p);
            caja.Focus();
            caja.SelectionStart = caja.Text.Length;
            if (Elegida != null) Elegida(p);
            if (TextoModificado != null) TextoModificado(this, EventArgs.Empty);
        }

        private void CajaTeclaPresionada(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && popup.Visible)
            {
                if (lista.SelectedIndex < lista.Items.Count - 1) lista.SelectedIndex++;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up && popup.Visible)
            {
                if (lista.SelectedIndex > 0) lista.SelectedIndex--;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                OcultarPopup();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (popup.Visible && lista.SelectedIndex >= 0 && lista.SelectedIndex < sugerencias.Length)
                {
                    Elegir(sugerencias[lista.SelectedIndex]);
                }
                else
                {
                    Parada p = ObtenerParada();
                    OcultarPopup();
                    if (p != null && Elegida != null) Elegida(p);
                }
            }
        }

        private void DibujarSugerencia(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= sugerencias.Length) return;
            Parada p = sugerencias[e.Index];
            bool activo = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (Brush fondo = new SolidBrush(activo ? Tema.Seleccion : Color.White))
                e.Graphics.FillRectangle(fondo, e.Bounds);

            using (Font f1 = Tema.FuenteNegrita(9.5f))
            using (Font f2 = Tema.Fuente(8.5f))
            using (Brush b1 = new SolidBrush(Tema.Texto))
            using (Brush b2 = new SolidBrush(Tema.TextoSuave))
            {
                e.Graphics.DrawString(p.Nombre, f1, b1, e.Bounds.X + 10, e.Bounds.Y + 5);
                e.Graphics.DrawString("Código " + p.Codigo, f2, b2, e.Bounds.X + 10, e.Bounds.Y + 23);
            }
            using (Pen borde = new Pen(Tema.Borde))
                e.Graphics.DrawLine(borde, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                popup.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
