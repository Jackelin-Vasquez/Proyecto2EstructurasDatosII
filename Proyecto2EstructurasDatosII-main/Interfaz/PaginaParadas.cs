using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Estructuras;
using Proyecto2EstructurasDatosII.Models;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Paradas: buscar (Trie + Tabla Hash), registrar y eliminar.
    public class PaginaParadas : PaginaBase
    {
        private readonly CajaAutocompletar buscador;
        private readonly Label lblNombre, lblDetalle;
        private readonly Button btnEliminar;
        private readonly TextBox txtCodigo, txtNombre, txtLat, txtLon;
        private readonly DataGridView grid;
        private readonly Label lblConteo;
        private Parada actual;
        private bool llenando;

        public PaginaParadas(Contexto ctx)
            : base(ctx, "Paradas", "Busque una parada mientras escribe, registre nuevas paradas o elimínelas de la red.")
        {
            TableLayoutPanel cols = Columnas(372);

            // ---------- Columna izquierda ----------
            TarjetaPanel izq = new TarjetaPanel("Buscar y registrar");
            FlowLayoutPanel pila = Pila();
            izq.Contenido.Controls.Add(pila);

            const int W = 326;

            pila.Controls.Add(Tema.Campo("Buscar parada por nombre o código"));
            buscador = new CajaAutocompletar();
            buscador.Width = W;
            buscador.Enlazar(ctx.Red);
            buscador.PistaTexto = "Ej.: Parque, Term, P03…";
            buscador.Elegida += delegate (Parada p) { Mostrar(p); SeleccionarEnTabla(p); };
            pila.Controls.Add(buscador);

            // Ficha de la parada encontrada
            Panel ficha = new Panel();
            ficha.Width = W;
            ficha.Height = 176;
            ficha.BackColor = Tema.Fondo;
            ficha.Margin = new Padding(0, 6, 0, 6);
            lblNombre = Tema.Etiqueta("Ninguna parada seleccionada", 11f, true, Tema.Texto);
            lblNombre.Location = new Point(14, 12);
            lblNombre.AutoSize = false;
            lblNombre.Size = new Size(W - 28, 24);
            lblNombre.AutoEllipsis = true;
            lblDetalle = Tema.Etiqueta("Escriba en el buscador o elija una fila de la tabla.", 9f, false, Tema.TextoSuave);
            lblDetalle.Location = new Point(14, 40);
            lblDetalle.AutoSize = false;
            lblDetalle.Size = new Size(W - 28, 126);
            ficha.Controls.Add(lblNombre);
            ficha.Controls.Add(lblDetalle);
            pila.Controls.Add(ficha);

            btnEliminar = Tema.BotonPeligro("Eliminar esta parada");
            btnEliminar.Width = W;
            btnEliminar.Enabled = false;
            btnEliminar.Click += EliminarClick;
            pila.Controls.Add(btnEliminar);

            Label sep = Tema.Titulo("Registrar nueva parada");
            sep.Margin = new Padding(0, 22, 0, 4);
            pila.Controls.Add(sep);

            pila.Controls.Add(Tema.Campo("Código (único)"));
            txtCodigo = Tema.Entrada(); txtCodigo.Width = W; pila.Controls.Add(txtCodigo);
            pila.Controls.Add(Tema.Campo("Nombre de la parada"));
            txtNombre = Tema.Entrada(); txtNombre.Width = W; pila.Controls.Add(txtNombre);

            TableLayoutPanel coords = new TableLayoutPanel();
            coords.Width = W;
            coords.Height = 66;
            coords.ColumnCount = 2;
            coords.RowCount = 2;
            coords.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            coords.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            coords.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            coords.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            coords.Margin = new Padding(0);
            Label l1 = Tema.Campo("Latitud"); l1.Margin = new Padding(0, 10, 0, 0);
            Label l2 = Tema.Campo("Longitud"); l2.Margin = new Padding(0, 10, 0, 0);
            txtLat = Tema.Entrada(); txtLat.Dock = DockStyle.Fill; txtLat.Margin = new Padding(0, 0, 8, 0);
            txtLon = Tema.Entrada(); txtLon.Dock = DockStyle.Fill; txtLon.Margin = new Padding(0);
            coords.Controls.Add(l1, 0, 0); coords.Controls.Add(l2, 1, 0);
            coords.Controls.Add(txtLat, 0, 1); coords.Controls.Add(txtLon, 1, 1);
            pila.Controls.Add(coords);

            Button btnRegistrar = Tema.Boton("Registrar parada", true);
            btnRegistrar.Width = W;
            btnRegistrar.Margin = new Padding(0, 16, 0, 0);
            btnRegistrar.Click += RegistrarClick;
            pila.Controls.Add(btnRegistrar);

            // ---------- Columna derecha ----------
            TarjetaPanel der = new TarjetaPanel("Paradas registradas");
            lblConteo = Tema.Etiqueta("", 9f, false, Tema.TextoSuave);
            lblConteo.Margin = new Padding(0, 6, 0, 0);
            der.Acciones.Controls.Add(lblConteo);

            grid = new DataGridView();
            Tema.EstilizarTabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.Columns.Add("codigo", "Código");
            grid.Columns.Add("nombre", "Nombre");
            grid.Columns.Add("lat", "Latitud");
            grid.Columns.Add("lon", "Longitud");
            grid.Columns.Add("cubeta", "Cubeta hash");
            grid.Columns.Add("zona", "Zona");
            grid.Columns[0].FillWeight = 14;
            grid.Columns[1].FillWeight = 40;
            grid.Columns[2].FillWeight = 17;
            grid.Columns[3].FillWeight = 17;
            grid.Columns[4].FillWeight = 14;
            grid.Columns[5].FillWeight = 10;
            grid.SelectionChanged += delegate
            {
                if (llenando || grid.CurrentRow == null) return;
                Parada p = Ctx.Red.Localizar(Convert.ToString(grid.CurrentRow.Cells[0].Value));
                if (p != null) Mostrar(p);
            };
            der.Contenido.Padding = new Padding(0);
            der.Contenido.Controls.Add(grid);

            cols.Controls.Add(izq, 0, 0);
            cols.Controls.Add(der, 2, 0);
            Cuerpo.Controls.Add(cols);
        }

        public override void Actualizar()
        {
            llenando = true;
            string codigoActual = actual == null ? null : actual.Codigo;

            Parada[] todas = Ctx.Red.ParadasOrdenadas();
            int[] zonas = Ctx.Red.NumerosDeZona(todas);

            grid.Rows.Clear();
            for (int i = 0; i < todas.Length; i++)
            {
                grid.Rows.Add(todas[i].Codigo, todas[i].Nombre,
                    todas[i].Latitud.ToString("0.0000", CultureInfo.InvariantCulture),
                    todas[i].Longitud.ToString("0.0000", CultureInfo.InvariantCulture),
                    Ctx.Red.Paradas.IndiceCubeta(todas[i].Codigo),
                    zonas[i]);
            }
            grid.ClearSelection();
            lblConteo.Text = todas.Length + (todas.Length == 1 ? " parada" : " paradas");
            llenando = false;

            // Conservar la ficha si la parada sigue existiendo.
            Parada vigente = codigoActual == null ? null : Ctx.Red.Localizar(codigoActual);
            Mostrar(vigente);
        }

        private void SeleccionarEnTabla(Parada p)
        {
            llenando = true;
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                if (Convert.ToString(grid.Rows[i].Cells[0].Value) == p.Codigo)
                {
                    grid.ClearSelection();
                    grid.Rows[i].Selected = true;
                    grid.CurrentCell = grid.Rows[i].Cells[0];
                    break;
                }
            }
            llenando = false;
        }

        // Ficha completa: el registro sale de la Tabla Hash; la zona, del Union-Find;
        // las rutas salientes, de la lista de adyacencia del Grafo.
        private void Mostrar(Parada p)
        {
            actual = p;
            btnEliminar.Enabled = p != null;

            if (p == null)
            {
                lblNombre.Text = "Ninguna parada seleccionada";
                lblDetalle.Text = "Escriba en el buscador o elija una fila de la tabla.";
                return;
            }

            Parada[] una = new Parada[] { p };
            int zona = Ctx.Red.NumerosDeZona(una)[0];
            AristaInfo[] salientes = Ctx.Red.Grafo.ObtenerAdyacentes(p.Codigo);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Código:   " + p.Codigo);
            sb.AppendLine("Posición: " + p.Latitud.ToString("0.0000", CultureInfo.InvariantCulture) + ", " +
                          p.Longitud.ToString("0.0000", CultureInfo.InvariantCulture));
            sb.AppendLine("Cubeta hash: " + Ctx.Red.Paradas.IndiceCubeta(p.Codigo) + " de " + Ctx.Red.Paradas.Capacidad);
            sb.AppendLine("Zona conectada: " + zona);
            sb.Append("Rutas salientes: " + salientes.Length);
            for (int i = 0; i < salientes.Length && i < 4; i++)
                sb.Append("\n   → " + salientes[i].Destino + "  (" + salientes[i].Tiempo + " min)");
            if (salientes.Length > 4) sb.Append("\n   … y " + (salientes.Length - 4) + " más");

            lblNombre.Text = p.Nombre;
            lblDetalle.Text = sb.ToString();
        }

        private void RegistrarClick(object sender, EventArgs e)
        {
            double lat, lon;
            if (!TryNumero(txtLat.Text, out lat) || !TryNumero(txtLon.Text, out lon))
            {
                Error("La latitud y la longitud deben ser números (ej.: 14.8447 y -91.5190).");
                return;
            }

            string error = Ctx.Red.RegistrarParada(txtCodigo.Text, txtNombre.Text, lat, lon);
            if (error != null) { Error(error); return; }

            Ctx.Avisar("Parada " + RedCodigo(txtCodigo.Text) + " registrada en la Tabla Hash, el Trie, el Grafo y el Union-Find.",
                       TipoMensaje.Exito);
            Parada nueva = Ctx.Red.Localizar(txtCodigo.Text);
            txtCodigo.Clear(); txtNombre.Clear(); txtLat.Clear(); txtLon.Clear();
            actual = nueva;
            Ctx.NotificarCambio();
            if (nueva != null) SeleccionarEnTabla(nueva);
            txtCodigo.Focus();
        }

        private static string RedCodigo(string c)
        {
            return Servicios.RedTransporte.NormalizarCodigo(c);
        }

        private void EliminarClick(object sender, EventArgs e)
        {
            if (actual == null) return;
            DialogResult r = MessageBox.Show(
                "¿Eliminar la parada " + actual.Codigo + " (" + actual.Nombre + ")?\n\n" +
                "También se eliminarán todas las rutas que pasan por ella.",
                "Eliminar parada", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;

            string codigo = actual.Codigo;
            int rutas;
            string error = Ctx.Red.EliminarParada(codigo, out rutas);
            if (error != null) { Error(error); return; }

            actual = null;
            buscador.Limpiar();
            Ctx.Avisar("Parada " + codigo + " eliminada junto con " + rutas + (rutas == 1 ? " ruta." : " rutas."),
                       TipoMensaje.Exito);
            Ctx.NotificarCambio();
        }

        private static bool TryNumero(string texto, out double valor)
        {
            return double.TryParse((texto ?? "").Trim().Replace(',', '.'),
                NumberStyles.Float, CultureInfo.InvariantCulture, out valor);
        }

        private void Error(string mensaje)
        {
            Ctx.Avisar(mensaje, TipoMensaje.Error);
            MessageBox.Show(mensaje, "No se pudo completar la operación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
