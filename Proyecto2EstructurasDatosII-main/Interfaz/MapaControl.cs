// Mapa esquemático de la red: dibuja paradas (coordenadas), rutas, zonas, caminos y recorridos.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Estructuras;
using Proyecto2EstructurasDatosII.Models;
using Proyecto2EstructurasDatosII.Servicios;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    public class MapaControl : Control
    {
        private RedTransporte red;
        private Parada[] paradas = new Parada[0];
        private PointF[] posiciones = new PointF[0];
        private int[] zonaDe = new int[0];
        private int[] tamanoZona = new int[0];
        private AristaInfo[] aristas = new AristaInfo[0];
        private int hover = -1;
        private Point cursor;

        // Vista: zoom y desplazamiento aplicados a las posiciones base.
        private float zoom = 1f;
        private PointF desplazamiento = PointF.Empty;
        private bool arrastrando;
        private bool huboArrastre;
        private Point origenArrastre;
        private PointF desplazamientoInicial;
        private const float ZoomMin = 1f, ZoomMax = 14f;

        // Estado visual configurable
        private string[] camino = new string[0];
        private string[] orden = new string[0];
        private string seleccionada;

        public bool ColorearZonas { get; set; }
        public bool MostrarEtiquetasRutas { get; set; }
        public bool MostrarLeyenda { get; set; }

        public event Action<Parada> ParadaSeleccionada;

        public MapaControl()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = ColorTranslator.FromHtml("#F7F9FC");
            Font = Tema.Fuente(9f);
            ColorearZonas = true;
            MostrarEtiquetasRutas = true;
            MostrarLeyenda = true;
        }

        public void Enlazar(RedTransporte r)
        {
            red = r;
            Recargar();
        }

        // Camino resaltado (Dijkstra): del primero (origen) al último (destino).
        public string[] Camino
        {
            get { return camino; }
            set { camino = value ?? new string[0]; Invalidate(); }
        }

        // Orden de visita (BFS / DFS): se dibuja un número sobre cada parada.
        public string[] OrdenVisita
        {
            get { return orden; }
            set { orden = value ?? new string[0]; Invalidate(); }
        }

        public string ParadaMarcada
        {
            get { return seleccionada; }
            set { seleccionada = value; Invalidate(); }
        }

        public void LimpiarResaltado()
        {
            camino = new string[0];
            orden = new string[0];
            seleccionada = null;
            Invalidate();
        }

        // Vuelve a leer paradas, rutas y zonas de la red.
        public void Recargar()
        {
            if (red == null) return;

            int anterior = paradas.Length;
            paradas = red.ParadasOrdenadas();
            aristas = red.Grafo.ObtenerAristas();
            if (paradas.Length != anterior) { zoom = 1f; desplazamiento = PointF.Empty; }

            Zona[] zonas = red.ObtenerZonas();
            zonaDe = new int[paradas.Length];
            tamanoZona = new int[zonas.Length];
            for (int z = 0; z < zonas.Length; z++)
            {
                tamanoZona[z] = zonas[z].Paradas.Length;
                for (int k = 0; k < zonas[z].Paradas.Length; k++)
                {
                    int pos = IndiceDe(zonas[z].Paradas[k].Codigo);
                    if (pos >= 0) zonaDe[pos] = z;
                }
            }
            CalcularPosiciones();
            hover = -1;
            Invalidate();
        }

        private PointF Transformar(PointF b)
        {
            return new PointF(b.X * zoom + desplazamiento.X, b.Y * zoom + desplazamiento.Y);
        }

        // Vuelve a encuadrar toda la red.
        public void AjustarVista()
        {
            zoom = 1f;
            desplazamiento = PointF.Empty;
            Invalidate();
        }

        private int IndiceDe(string codigo)
        {
            for (int i = 0; i < paradas.Length; i++)
                if (paradas[i].Codigo == codigo) return i;
            return -1;
        }

        private const int Margen = 46;

        private void CalcularPosiciones()
        {
            posiciones = new PointF[paradas.Length];
            if (paradas.Length == 0) return;

            double minLat = paradas[0].Latitud, maxLat = paradas[0].Latitud;
            double minLon = paradas[0].Longitud, maxLon = paradas[0].Longitud;
            for (int i = 1; i < paradas.Length; i++)
            {
                if (paradas[i].Latitud < minLat) minLat = paradas[i].Latitud;
                if (paradas[i].Latitud > maxLat) maxLat = paradas[i].Latitud;
                if (paradas[i].Longitud < minLon) minLon = paradas[i].Longitud;
                if (paradas[i].Longitud > maxLon) maxLon = paradas[i].Longitud;
            }

            double spanLat = Math.Max(maxLat - minLat, 0.0005);
            double spanLon = Math.Max(maxLon - minLon, 0.0005);
            float ancho = Math.Max(Width - 2 * Margen, 50);
            float alto = Math.Max(Height - 2 * Margen - 18, 50);

            // Se conserva la proporción para que el mapa no se deforme.
            double escala = Math.Min(ancho / spanLon, alto / spanLat);
            float offX = Margen + (float)((ancho - spanLon * escala) / 2);
            float offY = Margen + (float)((alto - spanLat * escala) / 2);

            for (int i = 0; i < paradas.Length; i++)
            {
                float x = offX + (float)((paradas[i].Longitud - minLon) * escala);
                float y = offY + (float)((maxLat - paradas[i].Latitud) * escala);
                posiciones[i] = new PointF(x, y);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CalcularPosiciones();
        }

        // ---------------------------------------------------------------
        //  Interacción
        // ---------------------------------------------------------------

        private int ParadaEn(Point p)
        {
            for (int i = paradas.Length - 1; i >= 0; i--)
            {
                PointF c = Transformar(posiciones[i]);
                float dx = c.X - p.X, dy = c.Y - p.Y;
                if (dx * dx + dy * dy <= 14 * 14) return i;
            }
            return -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && ParadaEn(e.Location) < 0)
            {
                arrastrando = true;
                huboArrastre = false;
                origenArrastre = e.Location;
                desplazamientoInicial = desplazamiento;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            arrastrando = false;
            if (Cursor == Cursors.SizeAll) Cursor = Cursors.Default;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float nuevo = e.Delta > 0 ? zoom * 1.25f : zoom / 1.25f;
            nuevo = Math.Max(ZoomMin, Math.Min(ZoomMax, nuevo));
            if (Math.Abs(nuevo - zoom) < 0.0001f) return;

            // El punto bajo el cursor se mantiene fijo mientras se acerca o aleja.
            float k = nuevo / zoom;
            desplazamiento = new PointF(e.X - (e.X - desplazamiento.X) * k, e.Y - (e.Y - desplazamiento.Y) * k);
            zoom = nuevo;
            if (zoom <= ZoomMin + 0.001f) desplazamiento = PointF.Empty;
            Invalidate();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (ParadaEn(e.Location) < 0) AjustarVista();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (arrastrando)
            {
                int dx = e.X - origenArrastre.X, dy = e.Y - origenArrastre.Y;
                if (huboArrastre || Math.Abs(dx) + Math.Abs(dy) > 4)
                {
                    huboArrastre = true;
                    Cursor = Cursors.SizeAll;
                    desplazamiento = new PointF(desplazamientoInicial.X + dx, desplazamientoInicial.Y + dy);
                    Invalidate();
                }
                return;
            }

            int h = ParadaEn(e.Location);
            cursor = e.Location;
            bool cambio = h != hover;
            if (cambio)
            {
                hover = h;
                Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
            }
            if (cambio || h >= 0) Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = -1;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (huboArrastre) { huboArrastre = false; return; }
            int i = ParadaEn(e.Location);
            if (i >= 0)
            {
                seleccionada = paradas[i].Codigo;
                Invalidate();
                if (ParadaSeleccionada != null) ParadaSeleccionada(paradas[i]);
            }
        }

        // ---------------------------------------------------------------
        //  Dibujo
        // ---------------------------------------------------------------

        private PointF PosDe(string codigo)
        {
            int i = IndiceDe(codigo);
            return i >= 0 ? Transformar(posiciones[i]) : PointF.Empty;
        }

        private int PosEnCamino(string codigo)
        {
            for (int i = 0; i < camino.Length; i++) if (camino[i] == codigo) return i;
            return -1;
        }

        private int PosEnOrden(string codigo)
        {
            for (int i = 0; i < orden.Length; i++) if (orden[i] == codigo) return i;
            return -1;
        }

        private bool AristaEnCamino(string a, string b)
        {
            for (int i = 0; i + 1 < camino.Length; i++)
                if (camino[i] == a && camino[i + 1] == b) return true;
            return false;
        }

        private bool ExisteInversa(AristaInfo a)
        {
            for (int i = 0; i < aristas.Length; i++)
                if (aristas[i].Origen == a.Destino && aristas[i].Destino == a.Origen) return true;
            return false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(BackColor);

            DibujarCuadricula(g);

            if (paradas.Length == 0)
            {
                using (Font f = Tema.Fuente(11f))
                using (Brush b = new SolidBrush(Tema.TextoSuave))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("Todavía no hay paradas en la red.\nCargue un archivo CSV o registre una parada.",
                                 f, b, new RectangleF(0, 0, Width, Height), sf);
                }
                return;
            }

            DibujarRutas(g);
            DibujarCamino(g);
            DibujarParadas(g);
            if (ColorearZonas && MostrarLeyenda) DibujarLeyenda(g);
            DibujarAyuda(g);
            DibujarHover(g);
        }

        private void DibujarCuadricula(Graphics g)
        {
            using (Pen p = new Pen(ColorTranslator.FromHtml("#E9EDF5")))
            {
                for (int x = 0; x < Width; x += 40) g.DrawLine(p, x, 0, x, Height);
                for (int y = 0; y < Height; y += 40) g.DrawLine(p, 0, y, Width, y);
            }
        }

        private void DibujarRutas(Graphics g)
        {
            using (Pen linea = new Pen(ColorTranslator.FromHtml("#AAB4C5"), 2f))
            using (Pen flecha = new Pen(ColorTranslator.FromHtml("#8793A8"), 2f))
            using (Font f = Tema.Fuente(8f))
            using (Brush txt = new SolidBrush(Tema.TextoSuave))
            using (Brush fondoTxt = new SolidBrush(Color.FromArgb(220, 247, 249, 252)))
            {
                flecha.CustomEndCap = new AdjustableArrowCap(4.5f, 5.5f);

                for (int i = 0; i < aristas.Length; i++)
                {
                    AristaInfo a = aristas[i];
                    bool doble = ExisteInversa(a);
                    // Las rutas de doble sentido se dibujan una sola vez.
                    if (doble && string.CompareOrdinal(a.Origen, a.Destino) > 0) continue;

                    PointF p1 = PosDe(a.Origen), p2 = PosDe(a.Destino);
                    if (doble)
                    {
                        g.DrawLine(linea, p1, p2);
                    }
                    else
                    {
                        PointF fin = Acortar(p1, p2, 11);
                        g.DrawLine(flecha, p1, fin);
                    }
                }
            }
        }

        // Acorta el segmento p1->p2 'dist' píxeles antes de p2 (para que la flecha no tape la parada).
        private static PointF Acortar(PointF p1, PointF p2, float dist)
        {
            float dx = p2.X - p1.X, dy = p2.Y - p1.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return p2;
            return new PointF(p2.X - dx / len * dist, p2.Y - dy / len * dist);
        }

        private void DibujarCamino(Graphics g)
        {
            if (camino.Length < 2) return;
            using (Pen p = new Pen(Tema.Primario, 5f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                for (int i = 0; i + 1 < camino.Length; i++)
                    g.DrawLine(p, PosDe(camino[i]), PosDe(camino[i + 1]));
            }
        }

        private void DibujarParadas(Graphics g)
        {
            int n = paradas.Length;
            float[] radios = new float[n];
            Color[] colores = new Color[n];
            PointF[] centros = new PointF[n];
            RectangleF[] ocupados = new RectangleF[n * 2 + aristas.Length + 1];
            int usados = 0;

            // 1) Círculos de las paradas.
            for (int i = 0; i < n; i++)
            {
                Parada p = paradas[i];
                PointF c = Transformar(posiciones[i]);
                centros[i] = c;
                int posCamino = PosEnCamino(p.Codigo);
                int posOrden = PosEnOrden(p.Codigo);

                Color relleno = ColorearZonas ? Tema.ColorZona(zonaDe[i]) : Tema.Primario;
                float radio = 9;
                if (camino.Length > 0)
                {
                    if (posCamino == 0) { relleno = Tema.Exito; radio = 12; }
                    else if (posCamino == camino.Length - 1) { relleno = Tema.Peligro; radio = 12; }
                    else if (posCamino > 0) { radio = 10; }
                    else { relleno = Color.FromArgb(150, relleno); }
                }
                if (posOrden >= 0) radio = 12;
                radios[i] = radio;
                colores[i] = relleno;

                if (p.Codigo == seleccionada || i == hover)
                {
                    using (Pen anillo = new Pen(Color.FromArgb(120, relleno), 4f))
                        g.DrawEllipse(anillo, c.X - radio - 4, c.Y - radio - 4, 2 * (radio + 4), 2 * (radio + 4));
                }

                using (Brush b = new SolidBrush(relleno))
                    g.FillEllipse(b, c.X - radio, c.Y - radio, 2 * radio, 2 * radio);
                using (Pen borde = new Pen(Color.White, 2f))
                    g.DrawEllipse(borde, c.X - radio, c.Y - radio, 2 * radio, 2 * radio);

                if (posOrden >= 0)
                {
                    using (Font fNum = Tema.FuenteNegrita(8.5f))
                    {
                        string num = (posOrden + 1).ToString();
                        SizeF sz = g.MeasureString(num, fNum);
                        g.DrawString(num, fNum, Brushes.White, c.X - sz.Width / 2 + 1, c.Y - sz.Height / 2 + 1);
                    }
                }

                ocupados[usados++] = new RectangleF(c.X - radio - 2, c.Y - radio - 2, 2 * radio + 4, 2 * radio + 4);
            }

            // 2) Nombres: primero las paradas importantes (camino, recorrido, selección) y
            //    luego el resto; una etiqueta se omite si taparía a otra (se ve al pasar el cursor).
            using (Font fNombre = Tema.Fuente(8.5f))
            using (Brush tNombre = new SolidBrush(Tema.Texto))
            using (Brush halo = new SolidBrush(Color.FromArgb(225, 247, 249, 252)))
            {
                for (int pasada = 0; pasada < 2; pasada++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        Parada p = paradas[i];
                        bool importante = p.Codigo == seleccionada || i == hover ||
                                          PosEnCamino(p.Codigo) >= 0 || PosEnOrden(p.Codigo) >= 0;
                        if ((pasada == 0) != importante) continue;

                        string etiqueta = p.Nombre.Length > 22 ? p.Nombre.Substring(0, 21) + "…" : p.Nombre;
                        SizeF tam = g.MeasureString(etiqueta, fNombre);
                        float w = tam.Width, h = tam.Height - 2, r = radios[i] + 3;
                        PointF c = centros[i];

                        RectangleF[] candidatos = new RectangleF[]
                        {
                            new RectangleF(c.X - w / 2, c.Y + r, w, h),          // abajo
                            new RectangleF(c.X - w / 2, c.Y - r - h, w, h),      // arriba
                            new RectangleF(c.X + r, c.Y - h / 2, w, h),          // derecha
                            new RectangleF(c.X - r - w, c.Y - h / 2, w, h)       // izquierda
                        };

                        for (int k = 0; k < candidatos.Length; k++)
                        {
                            if (HayChoque(candidatos[k], ocupados, usados)) continue;
                            g.FillRectangle(halo, candidatos[k]);
                            g.DrawString(etiqueta, fNombre, tNombre, candidatos[k].X, candidatos[k].Y);
                            ocupados[usados++] = candidatos[k];
                            break;
                        }
                    }
                }

                // 3) Tiempos sobre las rutas, solo donde hay espacio libre.
                if (MostrarEtiquetasRutas && aristas.Length <= 80)
                {
                    using (Font f = Tema.Fuente(8f))
                    using (Brush txt = new SolidBrush(Tema.TextoSuave))
                    using (Brush fondoTxt = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                    {
                        for (int i = 0; i < aristas.Length; i++)
                        {
                            AristaInfo a = aristas[i];
                            bool doble = ExisteInversa(a);
                            if (doble && string.CompareOrdinal(a.Origen, a.Destino) > 0) continue;

                            PointF p1 = PosDe(a.Origen), p2 = PosDe(a.Destino);
                            string t = a.Tiempo + " min";
                            SizeF s = g.MeasureString(t, f);
                            RectangleF r = new RectangleF((p1.X + p2.X) / 2 - s.Width / 2, (p1.Y + p2.Y) / 2 - s.Height / 2,
                                                          s.Width, s.Height);
                            if (HayChoque(r, ocupados, usados)) continue;
                            g.FillRectangle(fondoTxt, r);
                            g.DrawString(t, f, txt, r.X, r.Y);
                            ocupados[usados++] = r;
                        }
                    }
                }
            }
        }

        private static bool HayChoque(RectangleF r, RectangleF[] ocupados, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
                if (r.IntersectsWith(ocupados[i])) return true;
            return false;
        }

        private void DibujarAyuda(Graphics g)
        {
            string t = zoom > 1.01f ? "Zoom " + zoom.ToString("0.0") + "×  ·  doble clic: ajustar"
                                    : "Rueda: zoom  ·  arrastre: mover  ·  doble clic: ajustar";
            using (Font f = Tema.Fuente(8f))
            using (Brush b = new SolidBrush(Tema.TextoSuave))
            {
                SizeF s = g.MeasureString(t, f);
                g.DrawString(t, f, b, Width - s.Width - 10, Height - s.Height - 8);
            }
        }

        private void DibujarLeyenda(Graphics g)
        {
            int n = tamanoZona.Length;
            if (n == 0) return;
            int mostrar = Math.Min(n, Tema.Zonas.Length);
            using (Font f = Tema.Fuente(8.5f))
            using (Brush txt = new SolidBrush(Tema.Texto))
            using (Brush fondo = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
            using (Pen borde = new Pen(Tema.Borde))
            {
                float alto = 12 + mostrar * 19;
                // La leyenda va en la esquina que menos paradas tape.
                RectangleF[] esquinas = new RectangleF[]
                {
                    new RectangleF(10, Height - alto - 10, 150, alto),
                    new RectangleF(Width - 160, 10, 150, alto),
                    new RectangleF(10, 10, 150, alto),
                    new RectangleF(Width - 160, Height - alto - 28, 150, alto)
                };
                RectangleF r = esquinas[0];
                int menor = int.MaxValue;
                for (int k = 0; k < esquinas.Length; k++)
                {
                    RectangleF zona = RectangleF.Inflate(esquinas[k], 24, 24);
                    int tapadas = 0;
                    for (int i = 0; i < posiciones.Length; i++)
                        if (zona.Contains(Transformar(posiciones[i]))) tapadas++;
                    if (tapadas < menor) { menor = tapadas; r = esquinas[k]; }
                }
                g.FillRectangle(fondo, r);
                g.DrawRectangle(borde, r.X, r.Y, r.Width, r.Height);
                for (int z = 0; z < mostrar; z++)
                {
                    float y = r.Y + 8 + z * 19;
                    using (Brush b = new SolidBrush(Tema.ColorZona(z)))
                        g.FillEllipse(b, r.X + 10, y + 3, 10, 10);
                    g.DrawString("Zona " + (z + 1) + " · " + tamanoZona[z] + (tamanoZona[z] == 1 ? " parada" : " paradas"),
                                 f, txt, r.X + 26, y);
                }
            }
        }

        private void DibujarHover(Graphics g)
        {
            if (hover < 0 || hover >= paradas.Length) return;
            Parada p = paradas[hover];
            string linea1 = p.Codigo + " · " + p.Nombre;
            string linea2 = "Zona " + (zonaDe[hover] + 1) + "   (" + p.Latitud.ToString("0.0000") + ", " +
                            p.Longitud.ToString("0.0000") + ")";
            using (Font f1 = Tema.FuenteNegrita(9f))
            using (Font f2 = Tema.Fuente(8.5f))
            using (Brush fondo = new SolidBrush(Tema.Lateral))
            using (Brush b1 = new SolidBrush(Color.White))
            using (Brush b2 = new SolidBrush(Tema.LateralTexto))
            {
                SizeF s1 = g.MeasureString(linea1, f1), s2 = g.MeasureString(linea2, f2);
                float w = Math.Max(s1.Width, s2.Width) + 20, h = s1.Height + s2.Height + 12;
                float x = cursor.X + 16, y = cursor.Y + 16;
                if (x + w > Width) x = cursor.X - w - 8;
                if (y + h > Height) y = cursor.Y - h - 8;
                g.FillRectangle(fondo, x, y, w, h);
                g.DrawString(linea1, f1, b1, x + 10, y + 6);
                g.DrawString(linea2, f2, b2, x + 10, y + 6 + s1.Height);
            }
        }
    }
}
