using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Models;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Rutas: registrar (con verificación de zona por Union-Find) y eliminar.
    public class PaginaRutas : PaginaBase
    {
        private readonly CajaAutocompletar cajaOrigen, cajaDestino;
        private readonly NumericUpDown numTiempo, numCosto;
        private readonly CheckBox chkDoble;
        private readonly Label lblZona;
        private readonly Panel panelZona;
        private readonly DataGridView grid;
        private readonly Label lblConteo;

        public PaginaRutas(Contexto ctx)
            : base(ctx, "Rutas", "Conecte paradas con un tiempo y un costo de viaje; indique si operan en un solo sentido.")
        {
            TableLayoutPanel cols = Columnas(372);
            const int W = 326;

            TarjetaPanel izq = new TarjetaPanel("Registrar ruta");
            FlowLayoutPanel pila = Pila();
            izq.Contenido.Controls.Add(pila);

            pila.Controls.Add(Tema.Campo("Parada de origen"));
            cajaOrigen = new CajaAutocompletar();
            cajaOrigen.Width = W;
            cajaOrigen.Enlazar(ctx.Red);
            cajaOrigen.PistaTexto = "Escriba nombre o código…";
            cajaOrigen.TextoModificado += delegate { ActualizarEstadoZona(); };
            pila.Controls.Add(cajaOrigen);

            pila.Controls.Add(Tema.Campo("Parada de destino"));
            cajaDestino = new CajaAutocompletar();
            cajaDestino.Width = W;
            cajaDestino.Enlazar(ctx.Red);
            cajaDestino.PistaTexto = "Escriba nombre o código…";
            cajaDestino.TextoModificado += delegate { ActualizarEstadoZona(); };
            pila.Controls.Add(cajaDestino);

            TableLayoutPanel nums = new TableLayoutPanel();
            nums.Width = W;
            nums.Height = 66;
            nums.ColumnCount = 2;
            nums.RowCount = 2;
            nums.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            nums.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            nums.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            nums.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            nums.Margin = new Padding(0);
            Label a = Tema.Campo("Tiempo (minutos)"); a.Margin = new Padding(0, 10, 0, 0);
            Label b = Tema.Campo("Costo (Q)"); b.Margin = new Padding(0, 10, 0, 0);
            numTiempo = new NumericUpDown();
            numTiempo.Minimum = 0.5M; numTiempo.Maximum = 999; numTiempo.DecimalPlaces = 1; numTiempo.Value = 5;
            numTiempo.Font = Tema.Fuente(10f); numTiempo.Dock = DockStyle.Fill; numTiempo.Margin = new Padding(0, 0, 8, 0);
            numCosto = new NumericUpDown();
            numCosto.Minimum = 0; numCosto.Maximum = 9999; numCosto.DecimalPlaces = 2; numCosto.Value = 2;
            numCosto.Font = Tema.Fuente(10f); numCosto.Dock = DockStyle.Fill; numCosto.Margin = new Padding(0);
            nums.Controls.Add(a, 0, 0); nums.Controls.Add(b, 1, 0);
            nums.Controls.Add(numTiempo, 0, 1); nums.Controls.Add(numCosto, 1, 1);
            pila.Controls.Add(nums);

            chkDoble = new CheckBox();
            chkDoble.Text = "Ruta de doble sentido (ida y vuelta)";
            chkDoble.Checked = true;
            chkDoble.AutoSize = true;
            chkDoble.Font = Tema.Fuente(9.5f);
            chkDoble.ForeColor = Tema.Texto;
            chkDoble.Margin = new Padding(0, 14, 0, 0);
            pila.Controls.Add(chkDoble);

            // Resultado de la verificación con Union-Find
            panelZona = new Panel();
            panelZona.Width = W;
            panelZona.Height = 86;
            panelZona.Margin = new Padding(0, 16, 0, 0);
            panelZona.BackColor = Tema.Fondo;
            lblZona = Tema.Etiqueta("", 9f, false, Tema.TextoSuave);
            lblZona.Location = new Point(14, 10);
            lblZona.AutoSize = false;
            lblZona.Size = new Size(W - 28, 66);
            panelZona.Controls.Add(lblZona);
            pila.Controls.Add(panelZona);

            Button registrar = Tema.Boton("Registrar ruta", true);
            registrar.Width = W;
            registrar.Margin = new Padding(0, 14, 0, 0);
            registrar.Click += RegistrarClick;
            pila.Controls.Add(registrar);

            // Tabla
            TarjetaPanel der = new TarjetaPanel("Rutas registradas");
            lblConteo = Tema.Etiqueta("", 9f, false, Tema.TextoSuave);
            lblConteo.Margin = new Padding(0, 6, 12, 0);
            der.Acciones.Controls.Add(lblConteo);
            Button eliminar = Tema.BotonPeligro("Eliminar seleccionada");
            eliminar.Width = 160;
            eliminar.Height = 30;
            eliminar.Margin = new Padding(0, 0, 0, 0);
            eliminar.Click += EliminarClick;
            der.Acciones.Controls.Add(eliminar);

            grid = new DataGridView();
            Tema.EstilizarTabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.Columns.Add("origen", "Origen");
            grid.Columns.Add("destino", "Destino");
            grid.Columns.Add("tiempo", "Tiempo");
            grid.Columns.Add("costo", "Costo");
            grid.Columns.Add("sentido", "Sentido");
            grid.Columns[0].FillWeight = 30;
            grid.Columns[1].FillWeight = 30;
            grid.Columns[2].FillWeight = 11;
            grid.Columns[3].FillWeight = 11;
            grid.Columns[4].FillWeight = 18;
            der.Contenido.Padding = new Padding(0);
            der.Contenido.Controls.Add(grid);

            cols.Controls.Add(izq, 0, 0);
            cols.Controls.Add(der, 2, 0);
            Cuerpo.Controls.Add(cols);

            ActualizarEstadoZona();
        }

        public override void Actualizar()
        {
            Ruta[] rutas = Ctx.Red.ObtenerRutas();
            grid.Rows.Clear();
            for (int i = 0; i < rutas.Length; i++)
            {
                Parada o = Ctx.Red.Localizar(rutas[i].Origen);
                Parada d = Ctx.Red.Localizar(rutas[i].Destino);
                grid.Rows.Add(
                    rutas[i].Origen + " · " + (o == null ? "?" : o.Nombre),
                    rutas[i].Destino + " · " + (d == null ? "?" : d.Nombre),
                    rutas[i].Tiempo.ToString(CultureInfo.InvariantCulture) + " min",
                    "Q " + rutas[i].Costo.ToString("0.00", CultureInfo.InvariantCulture),
                    rutas[i].EsBidireccional ? "↔ Doble" : "→ Un sentido");
            }
            grid.ClearSelection();
            lblConteo.Text = rutas.Length + (rutas.Length == 1 ? " ruta" : " rutas");
            ActualizarEstadoZona();
        }

        // Union-Find: ¿ya están en la misma zona conectada?
        private void ActualizarEstadoZona()
        {
            Parada o = cajaOrigen.ObtenerParada();
            Parada d = cajaDestino.ObtenerParada();

            if (o == null || d == null)
            {
                Estado("Union-Find · elija las dos paradas para verificar si ya pertenecen a la misma zona conectada.",
                       Tema.Fondo, Tema.TextoSuave);
                return;
            }
            if (o == d)
            {
                Estado("El origen y el destino deben ser paradas distintas.",
                       ColorTranslator.FromHtml("#FDECEC"), Tema.Peligro);
                return;
            }

            bool? misma = Ctx.Red.EstanEnLaMismaZona(o.Codigo, d.Codigo);
            if (misma == true)
                Estado("Union-Find · " + o.Codigo + " y " + d.Codigo + " ya están en la misma zona conectada. " +
                       "La nueva ruta será un camino alternativo.",
                       ColorTranslator.FromHtml("#FEF6E7"), Tema.Alerta);
            else
                Estado("Union-Find · " + o.Codigo + " y " + d.Codigo + " están en zonas distintas. " +
                       "La nueva ruta las unirá en una sola zona.",
                       ColorTranslator.FromHtml("#E8F7EE"), Tema.Exito);
        }

        private void Estado(string texto, Color fondo, Color letra)
        {
            lblZona.Text = texto;
            lblZona.ForeColor = letra;
            panelZona.BackColor = fondo;
            lblZona.BackColor = fondo;
        }

        private void RegistrarClick(object sender, EventArgs e)
        {
            Parada o = cajaOrigen.ObtenerParada();
            Parada d = cajaDestino.ObtenerParada();
            if (o == null || d == null)
            {
                Error("Elija una parada de origen y una de destino que existan en la red.");
                return;
            }

            string error = Ctx.Red.RegistrarRuta(o.Codigo, d.Codigo, (double)numTiempo.Value,
                                                 (double)numCosto.Value, chkDoble.Checked);
            if (error != null) { Error(error); return; }

            Ctx.Avisar("Ruta " + o.Codigo + (chkDoble.Checked ? " ↔ " : " → ") + d.Codigo + " registrada.", TipoMensaje.Exito);
            cajaOrigen.Limpiar();
            cajaDestino.Limpiar();
            Ctx.NotificarCambio();
        }

        private void EliminarClick(object sender, EventArgs e)
        {
            if (grid.CurrentRow == null || !grid.CurrentRow.Selected)
            {
                Ctx.Avisar("Seleccione primero una ruta de la tabla.", TipoMensaje.Error);
                return;
            }
            int fila = grid.CurrentRow.Index;
            Ruta[] rutas = Ctx.Red.ObtenerRutas();
            if (fila < 0 || fila >= rutas.Length) return;

            DialogResult r = MessageBox.Show("¿Eliminar la ruta " + rutas[fila].Origen + " → " + rutas[fila].Destino + "?\n\n" +
                "Las zonas conectadas se recalcularán.", "Eliminar ruta",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;

            string error = Ctx.Red.EliminarRuta(fila);
            if (error != null) { Error(error); return; }
            Ctx.Avisar("Ruta eliminada y zonas recalculadas.", TipoMensaje.Exito);
            Ctx.NotificarCambio();
        }

        private void Error(string mensaje)
        {
            Ctx.Avisar(mensaje, TipoMensaje.Error);
            MessageBox.Show(mensaje, "No se pudo completar la operación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
