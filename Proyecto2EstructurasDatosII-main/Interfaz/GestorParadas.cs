// Alta y baja de paradas desde el mapa y desde el dibujo del grafo.
// Usa las mismas operaciones de RedTransporte que la pestaña «Paradas»: la parada se inserta
// (o se elimina) en la Tabla Hash, el Trie, el Grafo y el Union-Find a la vez.
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Models;
using Proyecto2EstructurasDatosII.Servicios;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    public static class GestorParadas
    {
        // Muestra el diálogo y registra la parada. Devuelve la parada creada, o null si se canceló.
        public static Parada Agregar(Contexto ctx, IWin32Window dueno, double latitud, double longitud)
        {
            using (DialogoParada dlg = new DialogoParada(ctx.Red, latitud, longitud))
            {
                if (dlg.ShowDialog(dueno) != DialogResult.OK) return null;

                Parada nueva = ctx.Red.Localizar(dlg.CodigoCreado);
                ctx.Avisar("Parada " + dlg.CodigoCreado + " agregada en (" +
                           nueva.Latitud.ToString("0.0000", CultureInfo.InvariantCulture) + ", " +
                           nueva.Longitud.ToString("0.0000", CultureInfo.InvariantCulture) +
                           "): Tabla Hash, Trie, Grafo y Union-Find actualizados.", TipoMensaje.Exito);
                ctx.NotificarCambio();
                return nueva;
            }
        }

        // Pide confirmación y elimina la parada con todas sus rutas.
        public static bool Eliminar(Contexto ctx, IWin32Window dueno, Parada p)
        {
            if (p == null) return false;

            int conectadas = ctx.Red.Grafo.ObtenerAdyacentes(p.Codigo).Length;
            DialogResult r = MessageBox.Show(dueno,
                "¿Eliminar la parada " + p.Codigo + " (" + p.Nombre + ")?\n\n" +
                "También se eliminarán las rutas que pasan por ella (" + conectadas + " saliente" +
                (conectadas == 1 ? "" : "s") + ") y se recalcularán las zonas conectadas.",
                "Eliminar parada", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return false;

            int rutas;
            string error = ctx.Red.EliminarParada(p.Codigo, out rutas);
            if (error != null)
            {
                ctx.Avisar(error, TipoMensaje.Error);
                return false;
            }

            ctx.Avisar("Parada " + p.Codigo + " eliminada junto con " + rutas + (rutas == 1 ? " ruta." : " rutas."),
                       TipoMensaje.Exito);
            ctx.NotificarCambio();
            return true;
        }

        // Punto de partida razonable cuando no hay un lugar elegido: el centro de las paradas actuales.
        public static void CentroDeLaRed(RedTransporte red, out double lat, out double lon)
        {
            Parada[] todas = red.ParadasOrdenadas();
            if (todas.Length == 0) { lat = 14.8347; lon = -91.5180; return; }   // Quetzaltenango
            double sLat = 0, sLon = 0;
            for (int i = 0; i < todas.Length; i++) { sLat += todas[i].Latitud; sLon += todas[i].Longitud; }
            lat = sLat / todas.Length;
            lon = sLon / todas.Length;
        }
    }

    // Formulario para completar los datos de una parada nueva.
    public class DialogoParada : Form
    {
        private readonly RedTransporte red;
        private readonly TextBox txtCodigo, txtNombre, txtLat, txtLon;
        private readonly Label lblError;

        public string CodigoCreado { get; private set; }

        public DialogoParada(RedTransporte red, double latitud, double longitud)
        {
            this.red = red;

            Text = "Agregar parada";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Tema.Tarjeta;
            Font = Tema.Fuente(9.5f);
            ClientSize = new Size(400, 400);

            Label titulo = Tema.Titulo("Nueva parada");
            titulo.Location = new Point(24, 18);
            Controls.Add(titulo);

            Label sub = Tema.Etiqueta("Se ubicará en el mapa en las coordenadas indicadas.", 9f, false, Tema.TextoSuave);
            sub.Location = new Point(24, 46);
            Controls.Add(sub);

            txtCodigo = Campo("Código", 78, SiguienteCodigo(red));
            txtNombre = Campo("Nombre", 142, "");
            txtLat = Campo("Latitud", 206, Formato(latitud));
            txtLat.Width = 168;

            Label lblLon = Tema.Campo("Longitud");
            lblLon.Location = new Point(24 + 168 + 16, 186);
            Controls.Add(lblLon);
            txtLon = Tema.Entrada();
            txtLon.Location = new Point(24 + 168 + 16, 206);
            txtLon.Width = 168;
            txtLon.Text = Formato(longitud);
            Controls.Add(txtLon);

            lblError = Tema.Etiqueta("", 9f, false, Tema.Peligro);
            lblError.AutoSize = false;
            lblError.Location = new Point(24, 252);
            lblError.Size = new Size(352, 44);
            Controls.Add(lblError);

            Button ok = Tema.Boton("Agregar parada", true);
            ok.Location = new Point(24, 316);
            ok.Size = new Size(200, 38);
            ok.Click += Aceptar;
            Controls.Add(ok);

            Button cancelar = Tema.Boton("Cancelar", false);
            cancelar.Location = new Point(236, 316);
            cancelar.Size = new Size(140, 38);
            cancelar.DialogResult = DialogResult.Cancel;
            Controls.Add(cancelar);

            AcceptButton = ok;
            CancelButton = cancelar;
            Shown += delegate { txtNombre.Focus(); };
        }

        private TextBox Campo(string etiqueta, int y, string valor)
        {
            Label l = Tema.Campo(etiqueta);
            l.Location = new Point(24, y - 20);
            Controls.Add(l);

            TextBox t = Tema.Entrada();
            t.Location = new Point(24, y);
            t.Width = 352;
            t.Text = valor;
            Controls.Add(t);
            return t;
        }

        private static string Formato(double v)
        {
            return v.ToString("0.000000", CultureInfo.InvariantCulture);
        }

        // Propone el siguiente código libre con el formato de los existentes (P01, P02...).
        public static string SiguienteCodigo(RedTransporte red)
        {
            int max = 0;
            Parada[] todas = red.ParadasOrdenadas();
            for (int i = 0; i < todas.Length; i++)
            {
                string c = todas[i].Codigo;
                if (c.Length > 1 && c[0] == 'P')
                {
                    int n;
                    if (int.TryParse(c.Substring(1), out n) && n > max) max = n;
                }
            }
            string candidato;
            do { max++; candidato = "P" + max.ToString("00"); } while (red.Localizar(candidato) != null);
            return candidato;
        }

        private static bool Numero(string texto, out double valor)
        {
            return double.TryParse((texto ?? "").Trim().Replace(',', '.'),
                NumberStyles.Float, CultureInfo.InvariantCulture, out valor);
        }

        private void Aceptar(object sender, EventArgs e)
        {
            double lat, lon;
            if (!Numero(txtLat.Text, out lat) || !Numero(txtLon.Text, out lon))
            {
                lblError.Text = "La latitud y la longitud deben ser números (ej.: 14.8447 y -91.5190).";
                return;
            }

            string error = red.RegistrarParada(txtCodigo.Text, txtNombre.Text, lat, lon);
            if (error != null)
            {
                lblError.Text = error;
                return;
            }

            CodigoCreado = RedTransporte.NormalizarCodigo(txtCodigo.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
