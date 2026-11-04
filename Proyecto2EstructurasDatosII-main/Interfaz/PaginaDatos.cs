using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Carga de datos desde archivos .csv / .txt, bitácora y datos de ejemplo.
    public class PaginaDatos : PaginaBase
    {
        private readonly TextBox txtParadas, txtRutas;
        private readonly ListBox bitacora;

        public PaginaDatos(Contexto ctx)
            : base(ctx, "Datos y archivos", "Cargue las paradas y rutas desde archivos CSV. Los datos no están escritos en el código.")
        {
            TableLayoutPanel cols = Columnas(540);
            const int W = 494;

            TarjetaPanel izq = new TarjetaPanel("Cargar archivos");
            FlowLayoutPanel pila = Pila();
            izq.Contenido.Controls.Add(pila);

            pila.Controls.Add(Tema.Campo("Archivo de paradas   (codigo,nombre,latitud,longitud)"));
            pila.Controls.Add(FilaArchivo(out txtParadas, W, "paradas.csv"));
            Button cargarP = Tema.Boton("Cargar paradas", true);
            cargarP.Width = W;
            cargarP.Margin = new Padding(0, 8, 0, 0);
            cargarP.Click += delegate { CargarParadas(txtParadas.Text); };
            pila.Controls.Add(cargarP);

            Label campoR = Tema.Campo("Archivo de rutas   (origen,destino,tiempo,costo,bidireccional)");
            campoR.Margin = new Padding(0, 22, 0, 2);
            pila.Controls.Add(campoR);
            pila.Controls.Add(FilaArchivo(out txtRutas, W, "rutas.csv"));
            Button cargarR = Tema.Boton("Cargar rutas", true);
            cargarR.Width = W;
            cargarR.Margin = new Padding(0, 8, 0, 0);
            cargarR.Click += delegate { CargarRutas(txtRutas.Text); };
            pila.Controls.Add(cargarR);

            Label nota = Tema.Etiqueta("Cargue primero las paradas y después las rutas: cada ruta debe unir paradas que ya existan.",
                                       9f, false, Tema.TextoSuave);
            nota.AutoSize = false;
            nota.Size = new Size(W, 34);
            nota.Margin = new Padding(0, 10, 0, 0);
            pila.Controls.Add(nota);

            Label sep = Tema.Titulo("Datos de ejemplo");
            sep.Margin = new Padding(0, 26, 0, 6);
            pila.Controls.Add(sep);

            Button ejemplo = Tema.Boton("Cargar datos de ejemplo (Quetzaltenango)", false);
            ejemplo.Width = W;
            ejemplo.Click += delegate { CargarEjemplo(false); };
            pila.Controls.Add(ejemplo);

            Button vaciar = Tema.BotonPeligro("Vaciar todos los datos");
            vaciar.Width = W;
            vaciar.Margin = new Padding(0, 8, 0, 0);
            vaciar.Click += VaciarClick;
            pila.Controls.Add(vaciar);

            TarjetaPanel der = new TarjetaPanel("Bitácora de carga");
            Button limpiar = Tema.Boton("Limpiar bitácora", false);
            limpiar.Width = 140;
            limpiar.Height = 30;
            limpiar.Click += delegate { bitacora.Items.Clear(); };
            der.Acciones.Controls.Add(limpiar);

            bitacora = new ListBox();
            Tema.EstilizarLista(bitacora);
            bitacora.Dock = DockStyle.Fill;
            bitacora.Font = Tema.FuenteMono(9f);
            bitacora.HorizontalScrollbar = true;
            der.Contenido.Padding = new Padding(10, 8, 10, 8);
            der.Contenido.Controls.Add(bitacora);

            cols.Controls.Add(izq, 0, 0);
            cols.Controls.Add(der, 2, 0);
            Cuerpo.Controls.Add(cols);
        }

        private Control FilaArchivo(out TextBox caja, int ancho, string sugerido)
        {
            TableLayoutPanel fila = new TableLayoutPanel();
            fila.Width = ancho;
            fila.Height = 34;
            fila.ColumnCount = 2;
            fila.RowCount = 1;
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            fila.Margin = new Padding(0);

            caja = Tema.Entrada();
            caja.Dock = DockStyle.Fill;
            caja.Margin = new Padding(0, 3, 8, 0);
            string ejemplo = RutaEjemplo(sugerido);
            if (File.Exists(ejemplo)) caja.Text = ejemplo;

            Button examinar = Tema.Boton("Examinar…", false);
            examinar.Dock = DockStyle.Fill;
            examinar.Margin = new Padding(0);
            TextBox destino = caja;
            examinar.Click += delegate
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Title = "Seleccione el archivo de datos";
                    dlg.Filter = "Archivos de datos (*.csv;*.txt)|*.csv;*.txt|Todos los archivos (*.*)|*.*";
                    if (dlg.ShowDialog() == DialogResult.OK) destino.Text = dlg.FileName;
                }
            };

            fila.Controls.Add(caja, 0, 0);
            fila.Controls.Add(examinar, 1, 0);
            return fila;
        }

        private static string RutaEjemplo(string archivo)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recursos", archivo);
        }

        public void Registrar(string mensaje)
        {
            bitacora.Items.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + mensaje);
            bitacora.TopIndex = Math.Max(0, bitacora.Items.Count - 1);
        }

        private bool ArchivoValido(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                Registrar("Indique la ruta de un archivo.");
                Ctx.Avisar("Indique la ruta del archivo que desea cargar.", TipoMensaje.Error);
                return false;
            }
            if (!File.Exists(ruta))
            {
                Registrar("No se encontró el archivo: " + ruta);
                Ctx.Avisar("No se encontró el archivo: " + ruta, TipoMensaje.Error);
                MessageBox.Show("No se encontró el archivo:\n" + ruta, "Archivo no encontrado",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void CargarParadas(string ruta)
        {
            if (!ArchivoValido(ruta)) return;
            try
            {
                Registrar("Leyendo paradas de " + Path.GetFileName(ruta) + "…");
                int n = Ctx.Red.CargarParadasCsv(ruta, Registrar);
                Registrar("✔ " + n + " paradas cargadas.");
                Ctx.Avisar(n + " paradas cargadas desde " + Path.GetFileName(ruta) + ".", TipoMensaje.Exito);
                Ctx.NotificarCambio();
            }
            catch (Exception ex)
            {
                Registrar("✖ Error al leer el archivo: " + ex.Message);
                Ctx.Avisar("Error al leer el archivo: " + ex.Message, TipoMensaje.Error);
            }
        }

        private void CargarRutas(string ruta)
        {
            if (!ArchivoValido(ruta)) return;
            try
            {
                Registrar("Leyendo rutas de " + Path.GetFileName(ruta) + "…");
                int n = Ctx.Red.CargarRutasCsv(ruta, Registrar);
                Registrar("✔ " + n + " rutas cargadas.");
                Ctx.Avisar(n + " rutas cargadas desde " + Path.GetFileName(ruta) + ".", TipoMensaje.Exito);
                Ctx.NotificarCambio();
            }
            catch (Exception ex)
            {
                Registrar("✖ Error al leer el archivo: " + ex.Message);
                Ctx.Avisar("Error al leer el archivo: " + ex.Message, TipoMensaje.Error);
            }
        }

        // 'silencioso' = al iniciar la aplicación (sin cuadros de diálogo si faltan los archivos).
        public void CargarEjemplo(bool silencioso)
        {
            string p = RutaEjemplo("paradas.csv");
            string r = RutaEjemplo("rutas.csv");
            if (!File.Exists(p) || !File.Exists(r))
            {
                Registrar("No se encontraron los datos de ejemplo en la carpeta Recursos.");
                if (!silencioso)
                    MessageBox.Show("No se encontraron los archivos de ejemplo en:\n" + Path.GetDirectoryName(p),
                                    "Datos de ejemplo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Ctx.Red.Reiniciar();
            txtParadas.Text = p;
            txtRutas.Text = r;
            CargarParadas(p);
            CargarRutas(r);
        }

        private void VaciarClick(object sender, EventArgs e)
        {
            DialogResult r = MessageBox.Show("¿Eliminar todas las paradas y rutas cargadas?", "Vaciar datos",
                                             MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
            Ctx.Red.Reiniciar();
            Registrar("Se vaciaron la Tabla Hash, el Grafo, el Trie y el Union-Find.");
            Ctx.Avisar("Todos los datos fueron eliminados.", TipoMensaje.Info);
            Ctx.NotificarCambio();
        }
    }
}
