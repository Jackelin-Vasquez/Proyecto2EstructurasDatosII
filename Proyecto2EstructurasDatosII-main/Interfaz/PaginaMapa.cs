using System;
using System.Drawing;
using System.Windows.Forms;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Vista general: indicadores de las cuatro estructuras y el mapa de la red.
    public class PaginaMapa : PaginaBase
    {
        private readonly TarjetaDato dParadas, dRutas, dZonas, dTrie;
        private readonly MapaControl mapa;
        private readonly CheckBox chkZonas, chkTiempos, chkReal;
        private readonly Button btnAgregar, btnEliminar;

        public PaginaMapa(Contexto ctx)
            : base(ctx, "Mapa de la red", "Vista general de las paradas, rutas y zonas conectadas de la ciudad.")
        {
            TableLayoutPanel raiz = new TableLayoutPanel();
            raiz.Dock = DockStyle.Fill;
            raiz.ColumnCount = 1;
            raiz.RowCount = 3;
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.BackColor = Tema.Fondo;

            TableLayoutPanel fila = new TableLayoutPanel();
            fila.Dock = DockStyle.Fill;
            fila.ColumnCount = 4;
            fila.RowCount = 1;
            for (int i = 0; i < 4; i++) fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            fila.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            fila.BackColor = Tema.Fondo;

            dParadas = new TarjetaDato("Paradas · Tabla Hash", Tema.Zonas[0]);
            dRutas = new TarjetaDato("Rutas · Grafo", Tema.Zonas[1]);
            dZonas = new TarjetaDato("Zonas · Union-Find", Tema.Zonas[2]);
            dTrie = new TarjetaDato("Nodos · Trie", Tema.Zonas[3]);
            dTrie.Margin = new Padding(0);
            fila.Controls.Add(dParadas, 0, 0);
            fila.Controls.Add(dRutas, 1, 0);
            fila.Controls.Add(dZonas, 2, 0);
            fila.Controls.Add(dTrie, 3, 0);

            TarjetaPanel tarjeta = new TarjetaPanel("Mapa de la ciudad");
            tarjeta.Contenido.Padding = new Padding(0);

            chkTiempos = new CheckBox();
            chkTiempos.Text = "Mostrar tiempos";
            chkTiempos.Checked = true;
            chkTiempos.AutoSize = true;
            chkTiempos.Font = Tema.Fuente(9f);
            chkTiempos.ForeColor = Tema.TextoSuave;
            chkTiempos.Margin = new Padding(0, 4, 16, 0);
            chkTiempos.CheckedChanged += delegate { mapa.MostrarEtiquetasRutas = chkTiempos.Checked; mapa.Invalidate(); };

            chkZonas = new CheckBox();
            chkZonas.Text = "Colorear por zona";
            chkZonas.Checked = true;
            chkZonas.AutoSize = true;
            chkZonas.Font = Tema.Fuente(9f);
            chkZonas.ForeColor = Tema.TextoSuave;
            chkZonas.Margin = new Padding(0, 4, 0, 0);
            chkZonas.CheckedChanged += delegate { mapa.ColorearZonas = chkZonas.Checked; mapa.Invalidate(); };

            chkReal = new CheckBox();
            chkReal.Text = "Mapa real";
            chkReal.Checked = true;
            chkReal.AutoSize = true;
            chkReal.Font = Tema.Fuente(9f);
            chkReal.ForeColor = Tema.TextoSuave;
            chkReal.Margin = new Padding(0, 4, 16, 0);
            chkReal.CheckedChanged += delegate { mapa.MapaReal = chkReal.Checked; mapa.AjustarVista(); };

            btnAgregar = Tema.Boton("＋ Agregar parada", true);
            btnAgregar.Size = new Size(150, 32);
            btnAgregar.Margin = new Padding(0, 0, 8, 0);
            btnAgregar.Click += delegate
            {
                if (!chkReal.Checked) chkReal.Checked = true;   // para ubicar con un clic hace falta el mapa real
                mapa.ModoAgregar = !mapa.ModoAgregar;
            };

            btnEliminar = Tema.BotonPeligro("Eliminar seleccionada");
            btnEliminar.Size = new Size(170, 32);
            btnEliminar.Margin = new Padding(0, 0, 16, 0);
            btnEliminar.Enabled = false;
            btnEliminar.Click += delegate { EliminarMarcada(); };

            tarjeta.Acciones.Controls.Add(btnAgregar);
            tarjeta.Acciones.Controls.Add(btnEliminar);
            tarjeta.Acciones.Controls.Add(chkReal);
            tarjeta.Acciones.Controls.Add(chkTiempos);
            tarjeta.Acciones.Controls.Add(chkZonas);

            mapa = new MapaControl();
            mapa.Dock = DockStyle.Fill;
            mapa.Enlazar(ctx.Red);
            mapa.ParadaSeleccionada += delegate (Proyecto2EstructurasDatosII.Models.Parada p)
            {
                ctx.Avisar("Parada seleccionada: " + p, TipoMensaje.Info);
                btnEliminar.Enabled = true;
            };
            mapa.UbicacionElegida += delegate (double lat, double lon)
            {
                // La parada queda exactamente en el punto donde se hizo clic en el mapa real.
                Proyecto2EstructurasDatosII.Models.Parada nueva = GestorParadas.Agregar(Ctx, FindForm(), lat, lon);
                if (nueva != null) { mapa.ParadaMarcada = nueva.Codigo; btnEliminar.Enabled = true; }
            };
            mapa.EliminarSolicitado += delegate (Proyecto2EstructurasDatosII.Models.Parada p)
            {
                GestorParadas.Eliminar(Ctx, FindForm(), p);
            };
            mapa.ModoAgregarCambiado += delegate
            {
                btnAgregar.Text = mapa.ModoAgregar ? "Cancelar (Esc)" : "＋ Agregar parada";
            };
            tarjeta.Contenido.Controls.Add(mapa);

            raiz.Controls.Add(fila, 0, 0);
            raiz.Controls.Add(tarjeta, 0, 2);
            Cuerpo.Controls.Add(raiz);
        }

        private void EliminarMarcada()
        {
            Proyecto2EstructurasDatosII.Models.Parada p = mapa.ParadaMarcada == null ? null : Ctx.Red.Localizar(mapa.ParadaMarcada);
            if (p == null) { btnEliminar.Enabled = false; return; }
            GestorParadas.Eliminar(Ctx, FindForm(), p);
        }

        public override void Actualizar()
        {
            Servicios.RedTransporte red = Ctx.Red;
            dParadas.Establecer(red.CantidadParadas.ToString(),
                red.Paradas.Capacidad + " cubetas · carga " + red.Paradas.FactorCarga.ToString("P0"));
            dRutas.Establecer(red.CantidadRutas.ToString(),
                red.Grafo.CantidadAristas + " aristas dirigidas en la lista de adyacencia");
            dZonas.Establecer(red.Zonas.Componentes.ToString(),
                red.Zonas.Componentes == 1 ? "toda la red está conectada" : "grupos de paradas conectadas");
            dTrie.Establecer(red.TrieNombres.ContarNodos().ToString(),
                red.TrieNombres.Cantidad + " nombres indexados para autocompletar");
            mapa.Recargar();
            btnEliminar.Enabled = mapa.ParadaMarcada != null && Ctx.Red.Localizar(mapa.ParadaMarcada) != null;
        }
    }
}
