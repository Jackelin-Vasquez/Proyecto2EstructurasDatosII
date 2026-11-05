// Dibujo del grafo (vértices, aristas con flecha y pesos) para la pestaña «Grafo».
// A diferencia del mapa, aquí no importan las coordenadas geográficas: los nodos se acomodan
// con un algoritmo de fuerzas (Fruchterman-Reingold) dentro de un recuadro por cada zona
// conectada (Union-Find), y el usuario puede arrastrarlos para reacomodarlos.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Estructuras;
using Proyecto2EstructurasDatosII.Models;
using Proyecto2EstructurasDatosII.Servicios;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    public class GrafoDibujo : Control
    {
        private RedTransporte red;
        private Parada[] paradas = new Parada[0];
        private AristaInfo[] aristas = new AristaInfo[0];
        private int[] zonaDe = new int[0];
        private int[] tamanoZona = new int[0];
        private PointF[] norm = new PointF[0];          // posición 0..1 dentro del recuadro de su zona
        private RectangleF[] recuadros = new RectangleF[0];

        private int hover = -1, seleccion = -1, arrastrando = -1;
        private bool huboArrastre;

        // El grafo no conoce coordenadas: la ventana de alta propone el centro de la red.
        public event Action AgregarSolicitado;
        public event Action<Parada> EliminarSolicitado;
        public event Action<Parada> ParadaElegida;

        // Parada marcada actualmente (o null).
        public Parada Marcada { get { return seleccion >= 0 && seleccion < paradas.Length ? paradas[seleccion] : null; } }

        // Marca un nodo por su código (p. ej. el recién creado).
        public void Marcar(string codigo)
        {
            seleccion = codigo == null ? -1 : IndiceDe(codigo);
            Invalidate();
        }

        private const float Radio = 18f;
        private const float PadX = 84f, PadArriba = 46f, PadAbajo = 52f;

        public GrafoDibujo()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Tema.Tarjeta;
            Font = Tema.Fuente(9f);
        }

        public void Enlazar(RedTransporte r)
        {
            red = r;
            Recargar();
        }

        // Vuelve a leer la red y acomoda los nodos desde cero.
        public void Recargar()
        {
            if (red == null) return;

            paradas = red.ParadasOrdenadas();
            aristas = red.Grafo.ObtenerAristas();
            seleccion = hover = arrastrando = -1;

            Zona[] zonas = red.ObtenerZonas();
            zonaDe = new int[paradas.Length];
            tamanoZona = new int[zonas.Length];
            norm = new PointF[paradas.Length];

            for (int z = 0; z < zonas.Length; z++)
            {
                int n = zonas[z].Paradas.Length;
                tamanoZona[z] = n;

                int[] miembros = new int[n];
                for (int k = 0; k < n; k++)
                {
                    miembros[k] = IndiceDe(zonas[z].Paradas[k].Codigo);
                    if (miembros[k] >= 0) zonaDe[miembros[k]] = z;
                }

                bool[,] ady = new bool[n, n];
                for (int a = 0; a < aristas.Length; a++)
                {
                    int u = Local(miembros, IndiceDe(aristas[a].Origen));
                    int v = Local(miembros, IndiceDe(aristas[a].Destino));
                    if (u >= 0 && v >= 0 && u != v) { ady[u, v] = true; ady[v, u] = true; }
                }

                PointF[] pos = Distribuir(n, ady);
                for (int k = 0; k < n; k++)
                    if (miembros[k] >= 0) norm[miembros[k]] = pos[k];
            }

            CalcularRecuadros();
            Invalidate();
        }

        private int IndiceDe(string codigo)
        {
            for (int i = 0; i < paradas.Length; i++)
                if (paradas[i].Codigo == codigo) return i;
            return -1;
        }

        private static int Local(int[] miembros, int global)
        {
            if (global < 0) return -1;
            for (int k = 0; k < miembros.Length; k++) if (miembros[k] == global) return k;
            return -1;
        }

        // ---------------------------------------------------------------
        //  Acomodo de nodos (Fruchterman-Reingold)
        // ---------------------------------------------------------------

        private static PointF[] Distribuir(int n, bool[,] ady)
        {
            PointF[] p = new PointF[n];
            if (n == 1) { p[0] = new PointF(0.5f, 0.5f); return p; }
            if (n == 2) { p[0] = new PointF(0.12f, 0.5f); p[1] = new PointF(0.88f, 0.5f); return p; }

            // Punto de partida determinista: los nodos sobre una circunferencia.
            for (int i = 0; i < n; i++)
            {
                double ang = 2 * Math.PI * i / n;
                p[i] = new PointF(0.5f + 0.4f * (float)Math.Cos(ang), 0.5f + 0.4f * (float)Math.Sin(ang));
            }

            const int Iteraciones = 300;
            double k = Math.Sqrt(1.0 / n);
            double[] dx = new double[n], dy = new double[n];

            for (int it = 0; it < Iteraciones; it++)
            {
                double temperatura = 0.1 * (1.0 - (double)it / Iteraciones) + 0.002;
                for (int i = 0; i < n; i++) { dx[i] = 0; dy[i] = 0; }

                // Repulsión entre todos los pares.
                for (int i = 0; i < n; i++)
                    for (int j = i + 1; j < n; j++)
                    {
                        double ex = p[i].X - p[j].X, ey = p[i].Y - p[j].Y;
                        double d = Math.Max(Math.Sqrt(ex * ex + ey * ey), 0.001);
                        double f = k * k / d;
                        dx[i] += ex / d * f; dy[i] += ey / d * f;
                        dx[j] -= ex / d * f; dy[j] -= ey / d * f;
                    }

                // Atracción entre nodos conectados.
                for (int i = 0; i < n; i++)
                    for (int j = i + 1; j < n; j++)
                    {
                        if (!ady[i, j]) continue;
                        double ex = p[i].X - p[j].X, ey = p[i].Y - p[j].Y;
                        double d = Math.Max(Math.Sqrt(ex * ex + ey * ey), 0.001);
                        double f = d * d / k;
                        dx[i] -= ex / d * f; dy[i] -= ey / d * f;
                        dx[j] += ex / d * f; dy[j] += ey / d * f;
                    }

                for (int i = 0; i < n; i++)
                {
                    double d = Math.Max(Math.Sqrt(dx[i] * dx[i] + dy[i] * dy[i]), 0.0001);
                    double paso = Math.Min(d, temperatura);
                    p[i] = new PointF((float)Math.Max(0, Math.Min(1, p[i].X + dx[i] / d * paso)),
                                      (float)Math.Max(0, Math.Min(1, p[i].Y + dy[i] / d * paso)));
                }
            }

            // Estirar el resultado para ocupar todo el recuadro.
            float minX = p[0].X, maxX = p[0].X, minY = p[0].Y, maxY = p[0].Y;
            for (int i = 1; i < n; i++)
            {
                minX = Math.Min(minX, p[i].X); maxX = Math.Max(maxX, p[i].X);
                minY = Math.Min(minY, p[i].Y); maxY = Math.Max(maxY, p[i].Y);
            }
            for (int i = 0; i < n; i++)
            {
                p[i] = new PointF(maxX - minX > 1e-5f ? (p[i].X - minX) / (maxX - minX) : 0.5f,
                                  maxY - minY > 1e-5f ? (p[i].Y - minY) / (maxY - minY) : 0.5f);
            }
            return p;
        }

        // Cada zona tiene su recuadro: la mayor ocupa la izquierda y las demás se apilan a la derecha.
        private void CalcularRecuadros()
        {
            int k = tamanoZona.Length;
            recuadros = new RectangleF[k];
            if (k == 0 || Width < 50 || Height < 50) return;

            const float m = 12f;
            if (k == 1) { recuadros[0] = new RectangleF(m, m, Width - 2 * m, Height - 2 * m); return; }

            int mayor = 0;
            for (int z = 1; z < k; z++) if (tamanoZona[z] > tamanoZona[mayor]) mayor = z;

            float anchoIzq = (Width - 3 * m) * 0.62f;
            recuadros[mayor] = new RectangleF(m, m, anchoIzq, Height - 2 * m);

            int otros = k - 1;
            float alto = (Height - m - otros * m) / otros;
            float x = 2 * m + anchoIzq, ancho = Width - x - m;
            int fila = 0;
            for (int z = 0; z < k; z++)
            {
                if (z == mayor) continue;
                recuadros[z] = new RectangleF(x, m + fila * (alto + m), ancho, alto);
                fila++;
            }
        }

        private PointF Pantalla(int i)
        {
            RectangleF r = recuadros[zonaDe[i]];
            float px = Math.Min(PadX, r.Width / 3), pArr = Math.Min(PadArriba, r.Height / 3), pAbj = Math.Min(PadAbajo, r.Height / 3);
            return new PointF(r.X + px + norm[i].X * (r.Width - 2 * px),
                              r.Y + pArr + norm[i].Y * (r.Height - pArr - pAbj));
        }

        private PointF Normalizar(int i, Point mouse)
        {
            RectangleF r = recuadros[zonaDe[i]];
            float px = Math.Min(PadX, r.Width / 3), pArr = Math.Min(PadArriba, r.Height / 3), pAbj = Math.Min(PadAbajo, r.Height / 3);
            float nx = (mouse.X - r.X - px) / Math.Max(r.Width - 2 * px, 1);
            float ny = (mouse.Y - r.Y - pArr) / Math.Max(r.Height - pArr - pAbj, 1);
            return new PointF(Math.Max(0, Math.Min(1, nx)), Math.Max(0, Math.Min(1, ny)));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CalcularRecuadros();
        }

        // ---------------------------------------------------------------
        //  Interacción
        // ---------------------------------------------------------------

        private int NodoEn(Point p)
        {
            if (recuadros.Length == 0) return -1;
            for (int i = paradas.Length - 1; i >= 0; i--)
            {
                PointF c = Pantalla(i);
                float dx = c.X - p.X, dy = c.Y - p.Y;
                if (dx * dx + dy * dy <= (Radio + 3) * (Radio + 3)) return i;
            }
            return -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int i = NodoEn(e.Location);
            if (e.Button == MouseButtons.Left && i >= 0)
            {
                arrastrando = i;
                huboArrastre = false;
                seleccion = i;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (arrastrando >= 0)
            {
                huboArrastre = true;
                norm[arrastrando] = Normalizar(arrastrando, e.Location);
                Cursor = Cursors.SizeAll;
                Invalidate();
                return;
            }

            int h = NodoEn(e.Location);
            if (h != hover)
            {
                hover = h;
                Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            arrastrando = -1;
            Cursor = hover >= 0 ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (huboArrastre) { huboArrastre = false; return; }
            int i = NodoEn(e.Location);
            seleccion = i;          // clic en el fondo = quitar selección
            Invalidate();
            if (e.Button == MouseButtons.Right) { MostrarMenu(e.Location, i); return; }
            if (i >= 0 && ParadaElegida != null) ParadaElegida(paradas[i]);
        }

        private void MostrarMenu(Point lugar, int i)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = Tema.Fuente(9.5f);
            if (i >= 0)
            {
                Parada p = paradas[i];
                ToolStripMenuItem cab = new ToolStripMenuItem(p.Codigo + " · " + p.Nombre);
                cab.Enabled = false;
                menu.Items.Add(cab);
                menu.Items.Add(new ToolStripSeparator());
                ToolStripMenuItem el = new ToolStripMenuItem("Eliminar esta parada…");
                el.ForeColor = Tema.Peligro;
                el.Click += delegate { if (EliminarSolicitado != null) EliminarSolicitado(p); };
                menu.Items.Add(el);
            }
            else
            {
                ToolStripMenuItem ag = new ToolStripMenuItem("Agregar parada…");
                ag.Click += delegate { if (AgregarSolicitado != null) AgregarSolicitado(); };
                menu.Items.Add(ag);
            }
            menu.Closed += delegate { BeginInvoke(new Action(menu.Dispose)); };
            menu.Show(this, lugar);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (NodoEn(e.Location) < 0) Recargar();   // reorganizar
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = -1;
            Invalidate();
        }

        // ---------------------------------------------------------------
        //  Dibujo
        // ---------------------------------------------------------------

        private bool ExisteInversa(AristaInfo a)
        {
            for (int i = 0; i < aristas.Length; i++)
                if (aristas[i].Origen == a.Destino && aristas[i].Destino == a.Origen) return true;
            return false;
        }

        private static PointF Acortar(PointF p1, PointF p2, float dist)
        {
            float dx = p2.X - p1.X, dy = p2.Y - p1.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return p2;
            return new PointF(p2.X - dx / len * dist, p2.Y - dy / len * dist);
        }

        private static bool HayChoque(RectangleF r, RectangleF[] ocupados, int cantidad)
        {
            for (int i = 0; i < cantidad; i++) if (r.IntersectsWith(ocupados[i])) return true;
            return false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(BackColor);

            if (paradas.Length == 0 || recuadros.Length == 0)
            {
                using (Font f = Tema.Fuente(11f))
                using (Brush b = new SolidBrush(Tema.TextoSuave))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("El grafo está vacío.\nCargue paradas y rutas para verlo dibujado.", f, b,
                                 new RectangleF(0, 0, Width, Height), sf);
                }
                return;
            }

            DibujarRecuadros(g);

            int foco = arrastrando >= 0 ? arrastrando : (hover >= 0 ? hover : seleccion);
            RectangleF[] ocupados = new RectangleF[paradas.Length * 2 + aristas.Length + 1];
            int usados = 0;

            DibujarAristas(g, foco);

            // Nodos
            PointF[] centros = new PointF[paradas.Length];
            for (int i = 0; i < paradas.Length; i++)
            {
                PointF c = Pantalla(i);
                centros[i] = c;
                Color relleno = Tema.ColorZona(zonaDe[i]);
                bool activo = i == foco;
                bool vecino = foco >= 0 && EsVecino(foco, i);
                if (foco >= 0 && !activo && !vecino) relleno = Color.FromArgb(120, relleno);

                if (activo || i == seleccion)
                {
                    using (Pen anillo = new Pen(Color.FromArgb(110, Tema.ColorZona(zonaDe[i])), 5f))
                        g.DrawEllipse(anillo, c.X - Radio - 4, c.Y - Radio - 4, 2 * (Radio + 4), 2 * (Radio + 4));
                }
                using (Brush b = new SolidBrush(relleno))
                    g.FillEllipse(b, c.X - Radio, c.Y - Radio, 2 * Radio, 2 * Radio);
                using (Pen borde = new Pen(Color.White, 2f))
                    g.DrawEllipse(borde, c.X - Radio, c.Y - Radio, 2 * Radio, 2 * Radio);

                using (Font f = Tema.FuenteNegrita(8f))
                {
                    SizeF sz = g.MeasureString(paradas[i].Codigo, f);
                    g.DrawString(paradas[i].Codigo, f, Brushes.White, c.X - sz.Width / 2 + 0.5f, c.Y - sz.Height / 2 + 1);
                }
                ocupados[usados++] = new RectangleF(c.X - Radio - 2, c.Y - Radio - 2, 2 * Radio + 4, 2 * Radio + 4);
            }

            // Nombres bajo cada nodo y pesos sobre las aristas, evitando que se encimen.
            using (Font fNombre = Tema.Fuente(8.5f))
            using (Brush tNombre = new SolidBrush(Tema.Texto))
            using (Brush halo = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
            {
                for (int i = 0; i < paradas.Length; i++)
                {
                    string et = paradas[i].Nombre.Length > 24 ? paradas[i].Nombre.Substring(0, 23) + "…" : paradas[i].Nombre;
                    SizeF tam = g.MeasureString(et, fNombre);
                    float w = tam.Width, h = tam.Height - 2;
                    PointF c = centros[i];
                    RectangleF[] cand = new RectangleF[]
                    {
                        new RectangleF(c.X - w / 2, c.Y + Radio + 3, w, h),
                        new RectangleF(c.X - w / 2, c.Y - Radio - 3 - h, w, h),
                        new RectangleF(c.X + Radio + 3, c.Y - h / 2, w, h),
                        new RectangleF(c.X - Radio - 3 - w, c.Y - h / 2, w, h)
                    };
                    for (int k = 0; k < cand.Length; k++)
                    {
                        if (HayChoque(cand[k], ocupados, usados)) continue;
                        g.FillRectangle(halo, cand[k]);
                        g.DrawString(et, fNombre, tNombre, cand[k].X, cand[k].Y);
                        ocupados[usados++] = cand[k];
                        break;
                    }
                }

                using (Font fPeso = Tema.Fuente(8f))
                using (Brush tPeso = new SolidBrush(Tema.TextoSuave))
                {
                    for (int i = 0; i < aristas.Length; i++)
                    {
                        AristaInfo a = aristas[i];
                        bool doble = ExisteInversa(a);
                        if (doble && string.CompareOrdinal(a.Origen, a.Destino) > 0) continue;

                        int u = IndiceDe(a.Origen), v = IndiceDe(a.Destino);
                        if (u < 0 || v < 0) continue;
                        string t = a.Tiempo.ToString("0.#") + " min · Q" + a.Costo.ToString("0.00");
                        SizeF sz = g.MeasureString(t, fPeso);
                        float mx = (centros[u].X + centros[v].X) / 2, my = (centros[u].Y + centros[v].Y) / 2;
                        RectangleF r = new RectangleF(mx - sz.Width / 2, my - sz.Height / 2, sz.Width, sz.Height - 2);
                        if (HayChoque(RectangleF.Inflate(r, 6, 2), ocupados, usados)) continue;
                        g.FillRectangle(halo, r);
                        g.DrawString(t, fPeso, tPeso, r.X, r.Y - 1);
                        ocupados[usados++] = r;
                    }
                }
            }

            DibujarInfo(g, foco);
            DibujarAyuda(g);
        }

        private bool EsVecino(int foco, int i)
        {
            string a = paradas[foco].Codigo, b = paradas[i].Codigo;
            for (int k = 0; k < aristas.Length; k++)
                if ((aristas[k].Origen == a && aristas[k].Destino == b) || (aristas[k].Origen == b && aristas[k].Destino == a))
                    return true;
            return false;
        }

        private void DibujarRecuadros(Graphics g)
        {
            using (Brush fondo = new SolidBrush(ColorTranslator.FromHtml("#F7F9FC")))
            using (Pen borde = new Pen(Tema.Borde))
            using (Font f = Tema.FuenteNegrita(9f))
            {
                for (int z = 0; z < recuadros.Length; z++)
                {
                    RectangleF r = recuadros[z];
                    g.FillRectangle(fondo, r);
                    g.DrawRectangle(borde, r.X, r.Y, r.Width, r.Height);
                    using (Brush barra = new SolidBrush(Tema.ColorZona(z)))
                    {
                        g.FillRectangle(barra, r.X, r.Y, 4, r.Height);
                        g.DrawString("Zona " + (z + 1) + " · " + tamanoZona[z] + (tamanoZona[z] == 1 ? " parada" : " paradas"),
                                     f, barra, r.X + 12, r.Y + 8);
                    }
                }
            }
        }

        private void DibujarAristas(Graphics g, int foco)
        {
            Color gris = ColorTranslator.FromHtml("#AAB4C5");
            Color grisFlecha = ColorTranslator.FromHtml("#8793A8");
            Color suave = ColorTranslator.FromHtml("#D5DBE6");

            for (int i = 0; i < aristas.Length; i++)
            {
                AristaInfo a = aristas[i];
                bool doble = ExisteInversa(a);
                if (doble && string.CompareOrdinal(a.Origen, a.Destino) > 0) continue;

                int u = IndiceDe(a.Origen), v = IndiceDe(a.Destino);
                if (u < 0 || v < 0) continue;

                bool resaltada = foco >= 0 && (u == foco || v == foco);
                bool atenuada = foco >= 0 && !resaltada;
                Color col = resaltada ? Tema.Primario : (atenuada ? suave : (doble ? gris : grisFlecha));

                using (Pen p = new Pen(col, resaltada ? 3.2f : 2.2f))
                {
                    PointF p1 = Pantalla(u), p2 = Pantalla(v);
                    if (doble) g.DrawLine(p, p1, p2);
                    else
                    {
                        p.CustomEndCap = new AdjustableArrowCap(4.5f, 5.5f);
                        g.DrawLine(p, p1, Acortar(p1, p2, Radio + 3));
                    }
                }
            }
        }

        // Detalle del nodo enfocado: a qué paradas llega y desde cuáles se llega.
        private void DibujarInfo(Graphics g, int foco)
        {
            if (foco < 0) return;
            string cod = paradas[foco].Codigo;
            string sale = "", entra = "";
            for (int k = 0; k < aristas.Length; k++)
            {
                if (aristas[k].Origen == cod) sale += (sale.Length > 0 ? ", " : "") + aristas[k].Destino;
                if (aristas[k].Destino == cod) entra += (entra.Length > 0 ? ", " : "") + aristas[k].Origen;
            }
            if (sale.Length == 0) sale = "ninguna";
            if (entra.Length == 0) entra = "ninguna";

            string l1 = cod + " · " + paradas[foco].Nombre;
            string l2 = "Salen rutas hacia: " + sale;
            string l3 = "Llegan rutas desde: " + entra;
            using (Font f1 = Tema.FuenteNegrita(9f))
            using (Font f2 = Tema.Fuente(8.5f))
            using (Brush fondo = new SolidBrush(Tema.Lateral))
            using (Brush b1 = new SolidBrush(Color.White))
            using (Brush b2 = new SolidBrush(Tema.LateralTexto))
            {
                SizeF s1 = g.MeasureString(l1, f1), s2 = g.MeasureString(l2, f2), s3 = g.MeasureString(l3, f2);
                float w = Math.Max(s1.Width, Math.Max(s2.Width, s3.Width)) + 22, h = s1.Height + s2.Height + s3.Height + 14;
                float x = 18, y = Height - h - 32;
                g.FillRectangle(fondo, x, y, w, h);
                g.DrawString(l1, f1, b1, x + 11, y + 6);
                g.DrawString(l2, f2, b2, x + 11, y + 6 + s1.Height);
                g.DrawString(l3, f2, b2, x + 11, y + 6 + s1.Height + s2.Height);
            }
        }

        private void DibujarAyuda(Graphics g)
        {
            string t = "Clic: ver conexiones  ·  arrastre un nodo para moverlo  ·  doble clic: reorganizar   (flecha = un solo sentido)";
            using (Font f = Tema.Fuente(8f))
            using (Brush b = new SolidBrush(Tema.TextoSuave))
            {
                SizeF s = g.MeasureString(t, f);
                g.DrawString(t, f, b, Width - s.Width - 14, Height - s.Height - 8);
            }
        }
    }
}
