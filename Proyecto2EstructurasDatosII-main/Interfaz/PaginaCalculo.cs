using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Estructuras;
using Proyecto2EstructurasDatosII.Models;
using Proyecto2EstructurasDatosII.Servicios;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Calcular ruta: Trie (autocompletar) -> Tabla Hash (registro) -> Union-Find (zona) -> Dijkstra.
    public class PaginaCalculo : PaginaBase
    {
        private readonly CajaAutocompletar cajaOrigen, cajaDestino;
        private readonly RadioButton rbTiempo, rbCosto;
        private readonly Label lblResumen, lblDetalle;
        private readonly ListBox listaCamino, listaTraza;
        private readonly MapaControl mapa;

        public PaginaCalculo(Contexto ctx)
            : base(ctx, "Buscar ruta", "Calcule el viaje más rápido o más económico entre dos paradas con Dijkstra.")
        {
            TableLayoutPanel cols = Columnas(372);
            const int W = 326;

            // ---------- Izquierda ----------
            TarjetaPanel izq = new TarjetaPanel("Planificar viaje");
            FlowLayoutPanel pila = Pila();
            izq.Contenido.Controls.Add(pila);

            pila.Controls.Add(Tema.Campo("Origen"));
            cajaOrigen = new CajaAutocompletar();
            cajaOrigen.Width = W;
            cajaOrigen.Enlazar(ctx.Red);
            cajaOrigen.PistaTexto = "Escriba nombre o código…";
            cajaOrigen.Elegida += delegate { };
            pila.Controls.Add(cajaOrigen);

            pila.Controls.Add(Tema.Campo("Destino"));
            cajaDestino = new CajaAutocompletar();
            cajaDestino.Width = W;
            cajaDestino.Enlazar(ctx.Red);
            cajaDestino.PistaTexto = "Escriba nombre o código…";
            cajaDestino.Elegida += delegate { Calcular(); };
            pila.Controls.Add(cajaDestino);

            Button intercambiar = Tema.Boton("⇅  Intercambiar origen y destino", false);
            intercambiar.Width = W;
            intercambiar.Height = 32;
            intercambiar.Margin = new Padding(0, 4, 0, 0);
            intercambiar.Click += delegate
            {
                Parada o = cajaOrigen.ObtenerParada();
                Parada d = cajaDestino.ObtenerParada();
                string to = cajaOrigen.Texto, td = cajaDestino.Texto;
                if (d != null) cajaOrigen.Establecer(d); else cajaOrigen.Texto = td;
                if (o != null) cajaDestino.Establecer(o); else cajaDestino.Texto = to;
            };
            pila.Controls.Add(intercambiar);

            pila.Controls.Add(Tema.Campo("Optimizar por"));
            rbTiempo = new RadioButton();
            rbTiempo.Text = "Ruta más rápida (menor tiempo)";
            rbTiempo.Checked = true;
            rbTiempo.AutoSize = true;
            rbTiempo.Font = Tema.Fuente(9.5f);
            rbTiempo.ForeColor = Tema.Texto;
            rbTiempo.Margin = new Padding(0, 2, 0, 2);
            rbCosto = new RadioButton();
            rbCosto.Text = "Ruta más económica (menor costo)";
            rbCosto.AutoSize = true;
            rbCosto.Font = Tema.Fuente(9.5f);
            rbCosto.ForeColor = Tema.Texto;
            rbCosto.Margin = new Padding(0, 2, 0, 2);
            pila.Controls.Add(rbTiempo);
            pila.Controls.Add(rbCosto);

            Button calcular = Tema.Boton("Calcular ruta", true);
            calcular.Width = W;
            calcular.Height = 40;
            calcular.Margin = new Padding(0, 14, 0, 0);
            calcular.Click += delegate { Calcular(); };
            pila.Controls.Add(calcular);

            // Resultado
            Panel resultado = new Panel();
            resultado.Width = W;
            resultado.Height = 74;
            resultado.Margin = new Padding(0, 16, 0, 0);
            resultado.BackColor = Tema.Fondo;
            lblResumen = Tema.Etiqueta("Sin calcular", 14f, true, Tema.Texto);
            lblResumen.Location = new Point(14, 10);
            lblResumen.AutoSize = false;
            lblResumen.Size = new Size(W - 28, 28);
            lblDetalle = Tema.Etiqueta("Elija origen y destino para ver el resultado.", 9f, false, Tema.TextoSuave);
            lblDetalle.Location = new Point(14, 40);
            lblDetalle.AutoSize = false;
            lblDetalle.Size = new Size(W - 28, 28);
            resultado.Controls.Add(lblResumen);
            resultado.Controls.Add(lblDetalle);
            pila.Controls.Add(resultado);

            Label tc = Tema.Campo("Recorrido paso a paso");
            tc.Margin = new Padding(0, 14, 0, 4);
            pila.Controls.Add(tc);
            listaCamino = new ListBox();
            Tema.EstilizarLista(listaCamino);
            listaCamino.Width = W;
            listaCamino.Height = 170;
            listaCamino.BackColor = Tema.Fondo;
            listaCamino.Margin = new Padding(0);
            pila.Controls.Add(listaCamino);

            // ---------- Derecha ----------
            TableLayoutPanel der = new TableLayoutPanel();
            der.Dock = DockStyle.Fill;
            der.ColumnCount = 1;
            der.RowCount = 3;
            der.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            der.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
            der.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
            der.BackColor = Tema.Fondo;

            TarjetaPanel tarjetaMapa = new TarjetaPanel("Ruta en el mapa");
            tarjetaMapa.Contenido.Padding = new Padding(0);
            mapa = new MapaControl();
            mapa.Dock = DockStyle.Fill;
            mapa.Enlazar(ctx.Red);
            mapa.MostrarLeyenda = false;
            tarjetaMapa.Contenido.Controls.Add(mapa);

            TarjetaPanel tarjetaTraza = new TarjetaPanel("Flujo entre estructuras");
            tarjetaTraza.Contenido.Padding = new Padding(0);
            listaTraza = new ListBox();
            Tema.EstilizarLista(listaTraza);
            listaTraza.Dock = DockStyle.Fill;
            listaTraza.Font = Tema.Fuente(9.5f);
            listaTraza.DrawMode = DrawMode.OwnerDrawFixed;
            listaTraza.ItemHeight = 26;
            listaTraza.DrawItem += DibujarTraza;
            tarjetaTraza.Contenido.Controls.Add(listaTraza);

            der.Controls.Add(tarjetaMapa, 0, 0);
            der.Controls.Add(tarjetaTraza, 0, 2);

            cols.Controls.Add(izq, 0, 0);
            cols.Controls.Add(der, 2, 0);
            Cuerpo.Controls.Add(cols);
        }

        public override void Actualizar()
        {
            mapa.Recargar();
            // Si el resultado anterior ya no es válido (se borraron datos), se limpia.
            Parada o = cajaOrigen.ObtenerParada();
            Parada d = cajaDestino.ObtenerParada();
            if (o == null || d == null) LimpiarResultado();
        }

        private void LimpiarResultado()
        {
            mapa.Camino = null;
            listaCamino.Items.Clear();
            listaTraza.Items.Clear();
            lblResumen.Text = "Sin calcular";
            lblResumen.ForeColor = Tema.Texto;
            lblDetalle.Text = "Elija origen y destino para ver el resultado.";
        }

        private void Calcular()
        {
            Parada po = cajaOrigen.ObtenerParada();
            Parada pd = cajaDestino.ObtenerParada();

            // Si se eligió de la lista del Trie se usa el código exacto (evita nombres repetidos).
            string o = po != null ? po.Codigo : cajaOrigen.Texto;
            string d = pd != null ? pd.Codigo : cajaDestino.Texto;

            Criterio criterio = rbTiempo.Checked ? Criterio.Tiempo : Criterio.Costo;
            ResultadoFlujo flujo = Ctx.Red.CalcularRuta(o, d, criterio);

            // Traza: se agrega el paso del Trie cuando la parada vino del autocompletado.
            listaTraza.Items.Clear();
            if (po != null && cajaOrigen.EligioSugerencia)
                listaTraza.Items.Add("Trie · el autocompletado sugirió «" + po.Nombre + "» para el origen.");
            if (pd != null && cajaDestino.EligioSugerencia)
                listaTraza.Items.Add("Trie · el autocompletado sugirió «" + pd.Nombre + "» para el destino.");
            for (int i = 0; i < flujo.Traza.Length; i++) listaTraza.Items.Add(flujo.Traza[i]);

            listaCamino.Items.Clear();

            if (flujo.Error != null)
            {
                lblResumen.Text = "Sin ruta disponible";
                lblResumen.ForeColor = Tema.Peligro;
                lblDetalle.Text = flujo.Error;
                mapa.Camino = null;
                Ctx.Avisar(flujo.Error, TipoMensaje.Error);
                return;
            }

            ResultadoRuta r = flujo.Ruta;
            lblResumen.Text = r.TiempoTotal.ToString("0.#", CultureInfo.InvariantCulture) + " min  ·  Q " +
                              r.CostoTotal.ToString("0.00", CultureInfo.InvariantCulture);
            lblResumen.ForeColor = Tema.Primario;
            lblDetalle.Text = (criterio == Criterio.Tiempo ? "Ruta más rápida" : "Ruta más económica") + " · " +
                              r.Camino.Length + " paradas · " + (r.Camino.Length - 1) + " tramos";

            for (int i = 0; i < r.Camino.Length; i++)
            {
                Parada p = Ctx.Red.Localizar(r.Camino[i]);
                listaCamino.Items.Add((i + 1) + ".  " + r.Camino[i] + "  " + (p == null ? "" : p.Nombre));
            }
            mapa.Camino = r.Camino;
            Ctx.Avisar("Ruta calculada: " + r.Camino[0] + " → " + r.Camino[r.Camino.Length - 1] + ".", TipoMensaje.Exito);
        }

        // Cada línea de la traza se dibuja con una etiqueta de color según la estructura.
        private void DibujarTraza(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            string texto = Convert.ToString(listaTraza.Items[e.Index]);
            int corte = texto.IndexOf(" · ", StringComparison.Ordinal);
            string estructura = corte > 0 ? texto.Substring(0, corte) : "";
            string detalle = corte > 0 ? texto.Substring(corte + 3) : texto;

            Color color = Tema.TextoSuave;
            if (estructura == "Trie") color = Tema.Zonas[3];
            else if (estructura == "Tabla Hash") color = Tema.Zonas[0];
            else if (estructura == "Union-Find") color = Tema.Zonas[2];
            else if (estructura == "Grafo") color = Tema.Zonas[1];

            e.Graphics.FillRectangle(Brushes.White, e.Bounds);
            using (Brush b = new SolidBrush(color))
            using (Font f = Tema.FuenteNegrita(8.5f))
            using (Brush t = new SolidBrush(Tema.Texto))
            using (Font f2 = Tema.Fuente(9f))
            {
                Rectangle etiqueta = new Rectangle(e.Bounds.X + 16, e.Bounds.Y + 4, 92, 18);
                e.Graphics.FillRectangle(b, etiqueta);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(estructura, f, Brushes.White, etiqueta, sf);
                e.Graphics.DrawString(detalle, f2, t, e.Bounds.X + 118, e.Bounds.Y + 5);
            }
        }
    }
}
