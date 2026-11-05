// Mapa esquemático de la red: dibuja paradas (coordenadas), rutas, zonas, caminos y recorridos.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
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

        // Mapa real (Web Mercator): coordenadas «mundo» 0..1, centro de la vista y nivel de zoom.
        private struct PuntoD { public double X, Y; }
        private PuntoD[] mundo = new PuntoD[0];
        private double centroX = 0.5, centroY = 0.5;
        private double zoomMapa = 12;
        private bool vistaManual;
        private double centroInicialX, centroInicialY;
        private int refrescoPendiente;
        private const double ZoomRealMin = 3, ZoomRealMax = 19;

        // true: mosaicos de OpenStreetMap como fondo (necesita Internet la primera vez).
        // false: mapa esquemático simple.
        public bool MapaReal { get; set; }

        // Estado visual configurable
        private string[] camino = new string[0];
        private string[] orden = new string[0];
        private string seleccionada;

        public bool ColorearZonas { get; set; }
        public bool MostrarEtiquetasRutas { get; set; }
        public bool MostrarLeyenda { get; set; }

        public event Action<Parada> ParadaSeleccionada;

        // Alta y baja de paradas directamente sobre el mapa.
        public event Action<double, double> UbicacionElegida;   // latitud, longitud del clic
        public event Action<Parada> EliminarSolicitado;
        public event Action ModoAgregarCambiado;

        private bool modoAgregar;

        // true: el siguiente clic sobre el mapa elige dónde va la nueva parada.
        public bool ModoAgregar
        {
            get { return modoAgregar; }
            set
            {
                if (value == modoAgregar) return;
                modoAgregar = value;
                Cursor = value ? Cursors.Cross : Cursors.Default;
                if (value) Focus();
                Invalidate();
                if (ModoAgregarCambiado != null) ModoAgregarCambiado();
            }
        }

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
            MapaReal = true;
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            MosaicosMapa.Actualizado += AlLlegarMosaico;
        }

        private void AlLlegarMosaico()
        {
            // Varias descargas pueden terminar casi a la vez: se agrupan en un solo repintado.
            if (Interlocked.Exchange(ref refrescoPendiente, 1) == 1) return;
            try
            {
                if (IsHandleCreated && !IsDisposed)
                    BeginInvoke(new Action(delegate
                    {
                        Interlocked.Exchange(ref refrescoPendiente, 0);
                        Invalidate();
                    }));
                else Interlocked.Exchange(ref refrescoPendiente, 0);
            }
            catch { Interlocked.Exchange(ref refrescoPendiente, 0); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) MosaicosMapa.Actualizado -= AlLlegarMosaico;
            base.Dispose(disposing);
        }

        private bool MapaRealActivo { get { return MapaReal && paradas.Length > 0; } }

        private double EscalaMundo { get { return 256.0 * Math.Pow(2, zoomMapa); } }

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
            // La vista solo se reencuadra en cargas grandes; agregar o quitar una parada no la mueve.
            if (anterior == 0 || Math.Abs(paradas.Length - anterior) > 1)
            { zoom = 1f; desplazamiento = PointF.Empty; vistaManual = false; }
            if (seleccionada != null && red.Localizar(seleccionada) == null) seleccionada = null;

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

        // Posición en pantalla de la parada i, según el modo (mapa real o esquema).
        private PointF Pantalla(int i)
        {
            if (MapaRealActivo && i < mundo.Length)
            {
                double s = EscalaMundo;
                return new PointF((float)((mundo[i].X - centroX) * s + Width / 2.0),
                                  (float)((mundo[i].Y - centroY) * s + Height / 2.0));
            }
            return Transformar(posiciones[i]);
        }

        // Vuelve a encuadrar toda la red.
        public void AjustarVista()
        {
            zoom = 1f;
            desplazamiento = PointF.Empty;
            vistaManual = false;
            AjustarReal();
            Invalidate();
        }

        // Elige centro y zoom para que quepan todas las paradas.
        private void AjustarReal()
        {
            if (mundo.Length == 0 || Width <= 0 || Height <= 0) return;

            double minX = mundo[0].X, maxX = mundo[0].X, minY = mundo[0].Y, maxY = mundo[0].Y;
            for (int i = 1; i < mundo.Length; i++)
            {
                if (mundo[i].X < minX) minX = mundo[i].X;
                if (mundo[i].X > maxX) maxX = mundo[i].X;
                if (mundo[i].Y < minY) minY = mundo[i].Y;
                if (mundo[i].Y > maxY) maxY = mundo[i].Y;
            }

            double ancho = Math.Max(Width - 2 * Margen, 50);
            double alto = Math.Max(Height - 2 * Margen, 50);
            double zx = (maxX - minX) > 1e-12 ? ancho / ((maxX - minX) * 256.0) : 1e9;
            double zy = (maxY - minY) > 1e-12 ? alto / ((maxY - minY) * 256.0) : 1e9;
            double z = Math.Log(Math.Min(zx, zy), 2);
            zoomMapa = Math.Max(ZoomRealMin, Math.Min(16, z));
            centroX = (minX + maxX) / 2;
            centroY = (minY + maxY) / 2;
        }

        // Latitud/longitud -> coordenadas «mundo» de Web Mercator (0..1).
        private static PuntoD Mercator(double lat, double lon)
        {
            lat = Math.Max(-85.0511, Math.Min(85.0511, lat));
            double rad = lat * Math.PI / 180.0;
            PuntoD p;
            p.X = (lon + 180.0) / 360.0;
            p.Y = (1.0 - Math.Log(Math.Tan(rad) + 1.0 / Math.Cos(rad)) / Math.PI) / 2.0;
            return p;
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
            mundo = new PuntoD[paradas.Length];
            if (paradas.Length == 0) return;

            for (int i = 0; i < paradas.Length; i++) mundo[i] = Mercator(paradas[i].Latitud, paradas[i].Longitud);
            if (!vistaManual) AjustarReal();

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
                PointF c = Pantalla(i);
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
                centroInicialX = centroX;
                centroInicialY = centroY;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            arrastrando = false;
            if (Cursor == Cursors.SizeAll) Cursor = modoAgregar ? Cursors.Cross : Cursors.Default;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (MapaRealActivo)
            {
                double nz = Math.Max(ZoomRealMin, Math.Min(ZoomRealMax, zoomMapa + (e.Delta > 0 ? 0.5 : -0.5)));
                if (Math.Abs(nz - zoomMapa) < 0.0001) return;

                // El punto bajo el cursor se mantiene fijo mientras se acerca o aleja.
                double s0 = EscalaMundo;
                double wx = centroX + (e.X - Width / 2.0) / s0, wy = centroY + (e.Y - Height / 2.0) / s0;
                zoomMapa = nz;
                double s1 = EscalaMundo;
                centroX = wx - (e.X - Width / 2.0) / s1;
                centroY = wy - (e.Y - Height / 2.0) / s1;
                vistaManual = true;
                Invalidate();
                return;
            }

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

            if (modoAgregar) { cursor = e.Location; Invalidate(); }

            if (arrastrando)
            {
                int dx = e.X - origenArrastre.X, dy = e.Y - origenArrastre.Y;
                if (huboArrastre || Math.Abs(dx) + Math.Abs(dy) > 4)
                {
                    huboArrastre = true;
                    Cursor = Cursors.SizeAll;
                    if (MapaRealActivo)
                    {
                        double s = EscalaMundo;
                        centroX = centroInicialX - dx / s;
                        centroY = Math.Max(0, Math.Min(1, centroInicialY - dy / s));
                        vistaManual = true;
                    }
                    else desplazamiento = new PointF(desplazamientoInicial.X + dx, desplazamientoInicial.Y + dy);
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
                Cursor = modoAgregar ? Cursors.Cross : (h >= 0 ? Cursors.Hand : Cursors.Default);
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

            if (e.Button == MouseButtons.Right) { MostrarMenu(e.Location); return; }
            if (e.Button != MouseButtons.Left) return;

            if (modoAgregar && MapaRealActivo)
            {
                double lat, lon;
                LatLonEn(e.Location, out lat, out lon);
                vistaManual = true;               // la vista se queda donde el usuario la dejó
                ModoAgregar = false;
                if (UbicacionElegida != null) UbicacionElegida(lat, lon);
                return;
            }

            int i = ParadaEn(e.Location);
            if (i >= 0)
            {
                seleccionada = paradas[i].Codigo;
                Invalidate();
                if (ParadaSeleccionada != null) ParadaSeleccionada(paradas[i]);
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Escape || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape && modoAgregar) ModoAgregar = false;
        }

        // Latitud y longitud del punto de pantalla p (inversa de la proyección Web Mercator).
        private void LatLonEn(Point p, out double lat, out double lon)
        {
            double s = EscalaMundo;
            double wx = centroX + (p.X - Width / 2.0) / s;
            double wy = centroY + (p.Y - Height / 2.0) / s;
            lon = wx * 360.0 - 180.0;
            lat = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * wy))) * 180.0 / Math.PI;
        }

        // Menú de clic derecho: agregar una parada aquí, o eliminar la parada bajo el cursor.
        private void MostrarMenu(Point lugar)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = Tema.Fuente(9.5f);
            int i = ParadaEn(lugar);

            if (i >= 0 && EliminarSolicitado == null) { menu.Dispose(); return; }   // este mapa es solo de consulta
            if (i >= 0)
            {
                Parada p = paradas[i];
                seleccionada = p.Codigo;
                Invalidate();

                ToolStripMenuItem cab = new ToolStripMenuItem(p.Codigo + " · " + p.Nombre);
                cab.Enabled = false;
                menu.Items.Add(cab);
                menu.Items.Add(new ToolStripSeparator());

                ToolStripMenuItem eliminar = new ToolStripMenuItem("Eliminar esta parada…");
                eliminar.ForeColor = Tema.Peligro;
                eliminar.Click += delegate
                {
                    vistaManual = true;
                    if (EliminarSolicitado != null) EliminarSolicitado(p);
                };
                menu.Items.Add(eliminar);
            }
            else if (UbicacionElegida != null)
            {
                if (MapaRealActivo)
                {
                    double lat, lon;
                    LatLonEn(lugar, out lat, out lon);
                    ToolStripMenuItem agregar = new ToolStripMenuItem("Agregar parada aquí…");
                    agregar.Click += delegate
                    {
                        vistaManual = true;
                        UbicacionElegida(lat, lon);
                    };
                    menu.Items.Add(agregar);

                    ToolStripMenuItem coord = new ToolStripMenuItem(
                        lat.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + ", " +
                        lon.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                    coord.Enabled = false;
                    menu.Items.Add(coord);
                }
                else
                {
                    ToolStripMenuItem aviso = new ToolStripMenuItem("Active «Mapa real» para ubicar paradas con un clic");
                    aviso.Enabled = false;
                    menu.Items.Add(aviso);
                }
            }

            if (menu.Items.Count == 0) { menu.Dispose(); return; }
            menu.Closed += delegate { BeginInvoke(new Action(menu.Dispose)); };
            menu.Show(this, lugar);
        }

        // ---------------------------------------------------------------
        //  Dibujo
        // ---------------------------------------------------------------

        private PointF PosDe(string codigo)
        {
            int i = IndiceDe(codigo);
            return i >= 0 ? Pantalla(i) : PointF.Empty;
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
            if (MapaRealActivo) DibujarMosaicos(g);

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
            if (modoAgregar) DibujarModoAgregar(g);
            else DibujarHover(g);
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
            bool real = MapaRealActivo;
            using (Pen contorno = new Pen(Color.FromArgb(210, 255, 255, 255), 5.5f))
            using (Pen linea = new Pen(real ? ColorTranslator.FromHtml("#5E6E8C") : ColorTranslator.FromHtml("#AAB4C5"), 2.2f))
            using (Pen flecha = new Pen(real ? ColorTranslator.FromHtml("#4B5B79") : ColorTranslator.FromHtml("#8793A8"), 2.2f))
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
                        if (real) g.DrawLine(contorno, p1, p2);
                        g.DrawLine(linea, p1, p2);
                    }
                    else
                    {
                        PointF fin = Acortar(p1, p2, 11);
                        if (real) g.DrawLine(contorno, p1, fin);
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
            using (Pen halo = new Pen(Color.FromArgb(220, 255, 255, 255), 9f))
            using (Pen p = new Pen(Tema.Primario, 5f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                halo.StartCap = LineCap.Round;
                halo.EndCap = LineCap.Round;
                for (int i = 0; i + 1 < camino.Length; i++)
                {
                    g.DrawLine(halo, PosDe(camino[i]), PosDe(camino[i + 1]));
                    g.DrawLine(p, PosDe(camino[i]), PosDe(camino[i + 1]));
                }
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
                PointF c = Pantalla(i);
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

        // Dibuja los mosaicos visibles de OpenStreetMap; los que faltan se descargan en segundo plano.
        private void DibujarMosaicos(Graphics g)
        {
            double s = EscalaMundo;
            int tz = (int)Math.Max(0, Math.Min(19, Math.Round(zoomMapa)));
            int n = 1 << tz;
            double tam = s / n;                         // tamaño en pantalla de un mosaico

            double x0 = centroX - Width / 2.0 / s, x1 = centroX + Width / 2.0 / s;
            double y0 = centroY - Height / 2.0 / s, y1 = centroY + Height / 2.0 / s;
            int tx0 = (int)Math.Floor(x0 * n), tx1 = (int)Math.Floor(x1 * n);
            int ty0 = Math.Max(0, (int)Math.Floor(y0 * n)), ty1 = Math.Min(n - 1, (int)Math.Floor(y1 * n));
            if ((tx1 - tx0 + 1) * (ty1 - ty0 + 1) > 120) return;   // zoom demasiado lejano

            InterpolationMode modo = g.InterpolationMode;
            PixelOffsetMode offset = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            int faltantes = 0;
            for (int tx = tx0; tx <= tx1; tx++)
            {
                for (int ty = ty0; ty <= ty1; ty++)
                {
                    float sx = (float)((tx / (double)n - centroX) * s + Width / 2.0);
                    float sy = (float)((ty / (double)n - centroY) * s + Height / 2.0);
                    Bitmap b = MosaicosMapa.Obtener(tz, ((tx % n) + n) % n, ty);
                    if (b == null) { faltantes++; continue; }
                    g.DrawImage(b, sx, sy, (float)tam + 1f, (float)tam + 1f);
                }
            }

            g.InterpolationMode = modo;
            g.PixelOffsetMode = offset;

            // Velo claro para que las rutas y paradas resalten sobre el mapa.
            using (Brush velo = new SolidBrush(Color.FromArgb(48, 255, 255, 255)))
                g.FillRectangle(velo, 0, 0, Width, Height);

            if (faltantes > 0)
            {
                string aviso = MosaicosMapa.HayFallosRecientes()
                    ? "Sin conexión: no se pudo cargar el mapa base (se muestran solo las paradas y rutas)."
                    : "Cargando mapa…";
                using (Font f = Tema.Fuente(8.5f))
                using (Brush fondo = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                using (Brush txt = new SolidBrush(MosaicosMapa.HayFallosRecientes() ? Tema.Alerta : Tema.TextoSuave))
                {
                    SizeF sz = g.MeasureString(aviso, f);
                    g.FillRectangle(fondo, 10, 10, sz.Width + 14, sz.Height + 8);
                    g.DrawString(aviso, f, txt, 17, 14);
                }
            }
        }

        // Marcador «fantasma» y coordenadas bajo el cursor mientras se elige dónde va la parada nueva.
        private void DibujarModoAgregar(Graphics g)
        {
            string aviso = "Haga clic en el mapa para ubicar la nueva parada   ·   Esc: cancelar";
            using (Font f = Tema.FuenteNegrita(9f))
            using (Brush fondo = new SolidBrush(Tema.Lateral))
            {
                SizeF sz = g.MeasureString(aviso, f);
                float x = (Width - sz.Width) / 2 - 12, y = 12;
                g.FillRectangle(fondo, x, y, sz.Width + 24, sz.Height + 12);
                g.DrawString(aviso, f, Brushes.White, x + 12, y + 6);
            }

            if (!MapaRealActivo || !ClientRectangle.Contains(cursor)) return;

            using (Pen anillo = new Pen(Color.FromArgb(150, Tema.Primario), 2f))
            using (Brush relleno = new SolidBrush(Color.FromArgb(120, Tema.Primario)))
            {
                anillo.DashStyle = DashStyle.Dash;
                g.FillEllipse(relleno, cursor.X - 9, cursor.Y - 9, 18, 18);
                g.DrawEllipse(anillo, cursor.X - 15, cursor.Y - 15, 30, 30);
            }

            double lat, lon;
            LatLonEn(cursor, out lat, out lon);
            string coord = lat.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + ", " +
                           lon.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
            using (Font f = Tema.Fuente(8.5f))
            using (Brush fondo = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
            using (Brush txt = new SolidBrush(Tema.Texto))
            {
                SizeF sz = g.MeasureString(coord, f);
                float x = cursor.X + 20, y = cursor.Y + 14;
                if (x + sz.Width + 8 > Width) x = cursor.X - sz.Width - 28;
                g.FillRectangle(fondo, x, y, sz.Width + 8, sz.Height + 2);
                g.DrawString(coord, f, txt, x + 4, y + 1);
            }
        }

        private void DibujarAyuda(Graphics g)
        {
            bool real = MapaRealActivo;
            string t = real ? "Rueda: zoom  ·  arrastre: mover  ·  doble clic: ajustar"
                            : (zoom > 1.01f ? "Zoom " + zoom.ToString("0.0") + "×  ·  doble clic: ajustar"
                                            : "Rueda: zoom  ·  arrastre: mover  ·  doble clic: ajustar");
            using (Font f = Tema.Fuente(8f))
            using (Brush b = new SolidBrush(Tema.TextoSuave))
            using (Brush fondo = new SolidBrush(Color.FromArgb(215, 255, 255, 255)))
            {
                SizeF s = g.MeasureString(t, f);
                float y = Height - s.Height - 8;

                if (real)
                {
                    // La atribución a OpenStreetMap es obligatoria por su licencia.
                    SizeF sa = g.MeasureString(MosaicosMapa.Atribucion, f);
                    g.FillRectangle(fondo, Width - sa.Width - 12, Height - sa.Height - 5, sa.Width + 12, sa.Height + 5);
                    g.DrawString(MosaicosMapa.Atribucion, f, b, Width - sa.Width - 6, Height - sa.Height - 3);
                    y -= sa.Height + 2;
                }

                g.FillRectangle(fondo, Width - s.Width - 14, y - 1, s.Width + 14, s.Height + 2);
                g.DrawString(t, f, b, Width - s.Width - 8, y);
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
                        if (zona.Contains(Pantalla(i))) tapadas++;
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
