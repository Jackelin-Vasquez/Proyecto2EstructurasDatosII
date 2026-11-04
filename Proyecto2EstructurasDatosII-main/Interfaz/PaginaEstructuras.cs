using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Estructuras;
using Proyecto2EstructurasDatosII.Models;

namespace Proyecto2EstructurasDatosII.Interfaz
{
    // Inspector de las cuatro estructuras: muestra su contenido interno y permite
    // probar sus operaciones (búsqueda, recorrido, impresión, compresión de camino...).
    public class PaginaEstructuras : PaginaBase
    {
        private readonly TabControl pestanas;

        // Tabla Hash
        private Label hashResumen, hashResultado;
        private TextBox hashCodigo;
        private DataGridView hashGrid;

        // Trie
        private TextBox triePrefijo, trieArbol;
        private ListBox trieLista;
        private RadioButton trieNombres, trieCodigos;
        private Label trieResumen;

        // Union-Find
        private Label ufResumen, ufResultado;
        private CajaAutocompletar ufCaja;
        private DataGridView ufGrid;

        // Grafo
        private Label grafoResumen;
        private ListBox grafoLista;

        public PaginaEstructuras(Contexto ctx)
            : base(ctx, "Estructuras de datos", "Contenido interno de cada estructura propia y pruebas de sus operaciones.")
        {
            pestanas = new TabControl();
            pestanas.Dock = DockStyle.Fill;
            pestanas.Font = Tema.Fuente(10f);
            pestanas.Padding = new Point(18, 6);
            pestanas.ItemSize = new Size(150, 34);
            pestanas.SizeMode = TabSizeMode.Fixed;

            pestanas.TabPages.Add(ConstruirHash());
            pestanas.TabPages.Add(ConstruirTrie());
            pestanas.TabPages.Add(ConstruirUnionFind());
            pestanas.TabPages.Add(ConstruirGrafo());
            Cuerpo.Controls.Add(pestanas);
        }

        public override void Actualizar()
        {
            ActualizarHash();
            ActualizarTrie();
            ActualizarUnionFind();
            ActualizarGrafo();
        }

        private TabPage NuevaPestana(string titulo)
        {
            TabPage t = new TabPage(titulo);
            t.BackColor = Tema.Fondo;
            t.Padding = new Padding(0, 14, 0, 0);
            return t;
        }

        private Label Responsable(string nombre, string descripcion)
        {
            Label l = Tema.Etiqueta("Responsable: " + nombre + "   ·   " + descripcion, 9f, false, Tema.TextoSuave);
            return l;
        }

        // ================= TABLA HASH =================
        private TabPage ConstruirHash()
        {
            TabPage t = NuevaPestana("Tabla Hash");

            TarjetaPanel tarjeta = new TarjetaPanel("Cubetas de la tabla (encadenamiento)");
            tarjeta.Contenido.Padding = new Padding(0);

            hashGrid = new DataGridView();
            Tema.EstilizarTabla(hashGrid);
            hashGrid.Dock = DockStyle.Fill;
            hashGrid.Columns.Add("c", "Cubeta");
            hashGrid.Columns.Add("n", "Elementos");
            hashGrid.Columns.Add("d", "Contenido de la cadena");
            hashGrid.Columns[0].FillWeight = 10;
            hashGrid.Columns[1].FillWeight = 12;
            hashGrid.Columns[2].FillWeight = 78;
            tarjeta.Contenido.Controls.Add(hashGrid);

            Panel arriba = new Panel();
            arriba.Dock = DockStyle.Top;
            arriba.Height = 112;
            arriba.BackColor = Tema.Fondo;

            Label resp = Responsable(Responsables.TablaHash, "clave: código · h = (h·31 + c) mod M · rehash al superar 75 % de carga");
            resp.Location = new Point(0, 0);
            hashResumen = Tema.Etiqueta("", 11f, true, Tema.Texto);
            hashResumen.Location = new Point(0, 26);

            hashCodigo = Tema.Entrada();
            hashCodigo.Location = new Point(0, 62);
            hashCodigo.Width = 200;
            hashCodigo.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { BuscarHash(); e.SuppressKeyPress = true; }
            };
            Button buscar = Tema.Boton("Buscar por código", true);
            buscar.Location = new Point(210, 59);
            buscar.Width = 150;
            buscar.Click += delegate { BuscarHash(); };
            hashResultado = Tema.Etiqueta("Escriba un código (ej.: P03) para localizarlo en la tabla.", 9.5f, false, Tema.TextoSuave);
            hashResultado.Location = new Point(372, 67);

            arriba.Controls.Add(resp);
            arriba.Controls.Add(hashResumen);
            arriba.Controls.Add(hashCodigo);
            arriba.Controls.Add(buscar);
            arriba.Controls.Add(hashResultado);

            t.Controls.Add(tarjeta);
            t.Controls.Add(arriba);
            return t;
        }

        private void ActualizarHash()
        {
            TablaHash h = Ctx.Red.Paradas;
            hashResumen.Text = h.Cantidad + " elementos  ·  " + h.Capacidad + " cubetas  ·  factor de carga " +
                               h.FactorCarga.ToString("0.00", CultureInfo.InvariantCulture);

            hashGrid.Rows.Clear();
            for (int i = 0; i < h.Capacidad; i++)
            {
                Parada[] cadena = h.ObtenerCubeta(i);
                StringBuilder sb = new StringBuilder();
                for (int k = 0; k < cadena.Length; k++)
                {
                    if (k > 0) sb.Append("  →  ");
                    sb.Append("[" + cadena[k].Codigo + " · " + cadena[k].Nombre + "]");
                }
                hashGrid.Rows.Add(i, cadena.Length, cadena.Length == 0 ? "—" : sb.ToString());
            }
            hashGrid.ClearSelection();
        }

        private void BuscarHash()
        {
            string codigo = hashCodigo.Text;
            if (string.IsNullOrWhiteSpace(codigo))
            {
                hashResultado.Text = "Escriba un código para buscar.";
                hashResultado.ForeColor = Tema.TextoSuave;
                return;
            }

            Parada p = Ctx.Red.Paradas.Buscar(codigo);
            int cubeta = Ctx.Red.Paradas.IndiceCubeta(codigo);
            if (p == null)
            {
                hashResultado.Text = "✖ No existe. Se revisó la cubeta " + cubeta + " (" +
                                     Ctx.Red.Paradas.ObtenerCubeta(cubeta).Length + " elementos en su cadena).";
                hashResultado.ForeColor = Tema.Peligro;
            }
            else
            {
                hashResultado.Text = "✔ " + p.Codigo + " · " + p.Nombre + " — cubeta " + cubeta +
                                     ", cadena de " + Ctx.Red.Paradas.ObtenerCubeta(cubeta).Length + ".";
                hashResultado.ForeColor = Tema.Exito;
                hashGrid.ClearSelection();
                if (cubeta >= 0 && cubeta < hashGrid.Rows.Count)
                {
                    hashGrid.Rows[cubeta].Selected = true;
                    hashGrid.FirstDisplayedScrollingRowIndex = Math.Max(0, cubeta - 3);
                }
            }
        }

        // ================= TRIE =================
        private TabPage ConstruirTrie()
        {
            TabPage t = NuevaPestana("Trie");

            Panel arriba = new Panel();
            arriba.Dock = DockStyle.Top;
            arriba.Height = 112;
            arriba.BackColor = Tema.Fondo;

            Label resp = Responsable(Responsables.Trie, "hijos como lista enlazada ordenada · sin tildes ni mayúsculas");
            resp.Location = new Point(0, 0);
            trieResumen = Tema.Etiqueta("", 11f, true, Tema.Texto);
            trieResumen.Location = new Point(0, 26);

            triePrefijo = Tema.Entrada();
            triePrefijo.Location = new Point(0, 62);
            triePrefijo.Width = 260;
            triePrefijo.TextChanged += delegate { BuscarPrefijo(); };

            trieNombres = new RadioButton();
            trieNombres.Text = "Nombres";
            trieNombres.Checked = true;
            trieNombres.AutoSize = true;
            trieNombres.Font = Tema.Fuente(9.5f);
            trieNombres.Location = new Point(280, 64);
            trieNombres.CheckedChanged += delegate { ActualizarTrie(); };
            trieCodigos = new RadioButton();
            trieCodigos.Text = "Códigos";
            trieCodigos.AutoSize = true;
            trieCodigos.Font = Tema.Fuente(9.5f);
            trieCodigos.Location = new Point(370, 64);

            arriba.Controls.Add(resp);
            arriba.Controls.Add(trieResumen);
            arriba.Controls.Add(triePrefijo);
            arriba.Controls.Add(trieNombres);
            arriba.Controls.Add(trieCodigos);

            TableLayoutPanel cols = new TableLayoutPanel();
            cols.Dock = DockStyle.Fill;
            cols.ColumnCount = 3;
            cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            cols.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
            cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            cols.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cols.BackColor = Tema.Fondo;

            TarjetaPanel izq = new TarjetaPanel("Búsqueda por prefijo");
            izq.Contenido.Padding = new Padding(0);
            trieLista = new ListBox();
            Tema.EstilizarLista(trieLista);
            trieLista.Dock = DockStyle.Fill;
            trieLista.ItemHeight = 22;
            izq.Contenido.Controls.Add(trieLista);

            TarjetaPanel der = new TarjetaPanel("Árbol impreso (cada letra es un nodo)");
            der.Contenido.Padding = new Padding(0);
            trieArbol = new TextBox();
            trieArbol.Multiline = true;
            trieArbol.ReadOnly = true;
            trieArbol.ScrollBars = ScrollBars.Both;
            trieArbol.WordWrap = false;
            trieArbol.BorderStyle = BorderStyle.None;
            trieArbol.BackColor = Color.White;
            trieArbol.Font = Tema.FuenteMono(9.5f);
            trieArbol.Dock = DockStyle.Fill;
            der.Contenido.Controls.Add(trieArbol);

            cols.Controls.Add(izq, 0, 0);
            cols.Controls.Add(der, 2, 0);

            t.Controls.Add(cols);
            t.Controls.Add(arriba);
            return t;
        }

        private Trie TrieActivo()
        {
            return trieNombres.Checked ? Ctx.Red.TrieNombres : Ctx.Red.TrieCodigos;
        }

        private void ActualizarTrie()
        {
            Trie tr = TrieActivo();
            trieResumen.Text = tr.Cantidad + " palabras  ·  " + tr.ContarNodos() + " nodos";
            string[] lineas = tr.Imprimir(600);
            trieArbol.Text = lineas.Length == 0 ? "(vacío)" : string.Join("\r\n", lineas);
            BuscarPrefijo();
        }

        private void BuscarPrefijo()
        {
            Trie tr = TrieActivo();
            Sugerencia[] s = triePrefijo.Text.Length == 0 ? tr.ObtenerTodas() : tr.BuscarPorPrefijo(triePrefijo.Text, 200);
            trieLista.BeginUpdate();
            trieLista.Items.Clear();
            for (int i = 0; i < s.Length; i++)
                trieLista.Items.Add("  " + s[i].Texto + "     →  " + s[i].Codigo);
            trieLista.EndUpdate();
        }

        // ================= UNION-FIND =================
        private TabPage ConstruirUnionFind()
        {
            TabPage t = NuevaPestana("Union-Find");

            Panel arriba = new Panel();
            arriba.Dock = DockStyle.Top;
            arriba.Height = 112;
            arriba.BackColor = Tema.Fondo;

            Label resp = Responsable(Responsables.UnionFind, "arreglos padre[] y rango[] · compresión de camino · unión por rango");
            resp.Location = new Point(0, 0);
            ufResumen = Tema.Etiqueta("", 11f, true, Tema.Texto);
            ufResumen.Location = new Point(0, 26);

            ufCaja = new CajaAutocompletar();
            ufCaja.Location = new Point(0, 58);
            ufCaja.Width = 260;
            ufCaja.Enlazar(Ctx.Red);
            ufCaja.PistaTexto = "Parada para Find…";
            Button find = Tema.Boton("Find con compresión", true);
            find.Location = new Point(270, 58);
            find.Width = 170;
            find.Click += delegate { EjecutarFind(); };
            ufResultado = Tema.Etiqueta("Elija una parada para ver el camino hasta su raíz antes y después de comprimir.", 9.5f, false, Tema.TextoSuave);
            ufResultado.Location = new Point(452, 67);

            arriba.Controls.Add(resp);
            arriba.Controls.Add(ufResumen);
            arriba.Controls.Add(ufCaja);
            arriba.Controls.Add(find);
            arriba.Controls.Add(ufResultado);

            TarjetaPanel tarjeta = new TarjetaPanel("Arreglos internos del Union-Find");
            tarjeta.Contenido.Padding = new Padding(0);
            ufGrid = new DataGridView();
            Tema.EstilizarTabla(ufGrid);
            ufGrid.Dock = DockStyle.Fill;
            ufGrid.Columns.Add("i", "Índice");
            ufGrid.Columns.Add("p", "Parada");
            ufGrid.Columns.Add("pa", "padre[i]");
            ufGrid.Columns.Add("r", "rango[i]");
            ufGrid.Columns.Add("z", "Raíz del conjunto");
            ufGrid.Columns[0].FillWeight = 10;
            ufGrid.Columns[1].FillWeight = 40;
            ufGrid.Columns[2].FillWeight = 14;
            ufGrid.Columns[3].FillWeight = 14;
            ufGrid.Columns[4].FillWeight = 22;
            tarjeta.Contenido.Controls.Add(ufGrid);

            t.Controls.Add(tarjeta);
            t.Controls.Add(arriba);
            return t;
        }

        private void ActualizarUnionFind()
        {
            UnionFind uf = Ctx.Red.Zonas;
            ufResumen.Text = uf.Tamano + " elementos  ·  " + uf.Componentes + " conjuntos (zonas)";

            Parada[] todas = Ctx.Red.Paradas.ObtenerTodas();
            ufGrid.Rows.Clear();
            for (int i = 0; i < uf.Tamano; i++)
            {
                string nombre = "";
                for (int k = 0; k < todas.Length; k++)
                    if (todas[k].Indice == i) { nombre = todas[k].Codigo + " · " + todas[k].Nombre; break; }

                int raiz = i;
                while (uf.PadreDe(raiz) != raiz) raiz = uf.PadreDe(raiz);   // sin comprimir
                ufGrid.Rows.Add(i, nombre, uf.PadreDe(i), uf.RangoDe(i), raiz);
            }
            ufGrid.ClearSelection();
        }

        private string CadenaHastaRaiz(int inicio)
        {
            UnionFind uf = Ctx.Red.Zonas;
            StringBuilder sb = new StringBuilder();
            int x = inicio;
            while (true)
            {
                sb.Append(Ctx.Red.CodigoDeIndice(x));
                int p = uf.PadreDe(x);
                if (p == x) { sb.Append(" (raíz)"); break; }
                sb.Append(" → ");
                x = p;
            }
            return sb.ToString();
        }

        private void EjecutarFind()
        {
            Parada p = ufCaja.ObtenerParada();
            if (p == null)
            {
                ufResultado.Text = "Elija una parada que exista en la red.";
                ufResultado.ForeColor = Tema.Peligro;
                return;
            }

            string antes = CadenaHastaRaiz(p.Indice);
            Ctx.Red.Zonas.Buscar(p.Indice);                // aquí ocurre la compresión de camino
            string despues = CadenaHastaRaiz(p.Indice);

            ufResultado.Text = "Antes: " + antes + "      Después: " + despues;
            ufResultado.ForeColor = Tema.Texto;
            ActualizarUnionFind();
        }

        // ================= GRAFO =================
        private TabPage ConstruirGrafo()
        {
            TabPage t = NuevaPestana("Grafo");

            Panel arriba = new Panel();
            arriba.Dock = DockStyle.Top;
            arriba.Height = 62;
            arriba.BackColor = Tema.Fondo;

            Label resp = Responsable(Responsables.Grafo, "lista de adyacencia · BFS con cola · DFS recursivo · Dijkstra con montículo propio");
            resp.Location = new Point(0, 0);
            grafoResumen = Tema.Etiqueta("", 11f, true, Tema.Texto);
            grafoResumen.Location = new Point(0, 26);
            arriba.Controls.Add(resp);
            arriba.Controls.Add(grafoResumen);

            TarjetaPanel tarjeta = new TarjetaPanel("Lista de adyacencia (un vértice por línea)");
            tarjeta.Contenido.Padding = new Padding(0);
            grafoLista = new ListBox();
            Tema.EstilizarLista(grafoLista);
            grafoLista.Dock = DockStyle.Fill;
            grafoLista.Font = Tema.FuenteMono(9.5f);
            grafoLista.ItemHeight = 22;
            tarjeta.Contenido.Controls.Add(grafoLista);

            t.Controls.Add(tarjeta);
            t.Controls.Add(arriba);
            return t;
        }

        private void ActualizarGrafo()
        {
            Grafo g = Ctx.Red.Grafo;
            grafoResumen.Text = g.CantidadVertices + " vértices  ·  " + g.CantidadAristas + " aristas dirigidas";
            string[] lineas = g.DescribirAdyacencia();
            grafoLista.BeginUpdate();
            grafoLista.Items.Clear();
            for (int i = 0; i < lineas.Length; i++) grafoLista.Items.Add("  " + lineas[i]);
            grafoLista.EndUpdate();
        }
    }
}
