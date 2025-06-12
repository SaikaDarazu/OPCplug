using OPCplug.Code;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPCplug
{
    public partial class Ventana_clientes : Form
    {
        private readonly Clientes _instancia_clientes;

        public Ventana_clientes(Clientes instancia_clientes)
        {
            _instancia_clientes = instancia_clientes;

            _instancia_clientes.Actualizar_valor_ui += Instancia_clientes_actualizar_ui;

            InitializeDynamicComponents();

            this.MinimumSize = new Size(800, 600);
            this.Size = new Size(800, 600);
            this.Text = "OPCplug - Visor de Clientes";
        }

        /// <summary>
        /// Crea y configura TODOS los controles dinámicamente.
        /// </summary>
        private void InitializeDynamicComponents()
        {
            // --- SOLUCIÓN AL ERROR: Crear el SplitContainer dinámicamente ---
            var splitContainer = new SplitContainer
            {
                Name = "splitContainer1",
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 40 // Un tamaño inicial para el panel superior
            };

            // --- Panel Superior (ComboBox) ---
            var comboBoxClients = new ComboBox
            {
                Name = "comboBoxClients",
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(5)
            };
            var label = new Label
            {
                Text = "Seleccionar Cliente:",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(5, 8, 0, 0)
            };

            // Añadimos los controles al Panel1 del SplitContainer.
            splitContainer.Panel1.Controls.Add(comboBoxClients);
            splitContainer.Panel1.Controls.Add(label);

            // --- Panel Inferior (DataGridView) ---
            var dgvNodes = new DataGridView
            {
                Name = "dgvNodes",
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            SetupDataGridViewColumns(dgvNodes);

            // Añadimos la tabla al Panel2 del SplitContainer.
            splitContainer.Panel2.Controls.Add(dgvNodes);

            // --- Añadimos el SplitContainer principal al formulario ---
            this.Controls.Add(splitContainer);

            // --- Enlazar Eventos ---
            this.Load += (s, e) => PopulateClientComboBox();
            comboBoxClients.SelectedIndexChanged += (s, e) => PopulateNodeGrid();
            dgvNodes.CellClick += async (s, e) => await DataGridView_CellClick(s, e);
            this.FormClosing += (s, e) => _instancia_clientes.Actualizar_valor_ui -= Instancia_clientes_actualizar_ui;
        }

        private void SetupDataGridViewColumns(DataGridView dgv)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "NodeName", HeaderText = "Nombre Variable", ReadOnly = true, FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReadValue", HeaderText = "Valor Lectura", ReadOnly = true, FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "WriteValue", HeaderText = "Valor Escritura", FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewButtonColumn { Name = "WriteButton", HeaderText = "Acción", Text = "Escribir", UseColumnTextForButtonValue = true, FillWeight = 20 });
        }

        private void PopulateClientComboBox()
        {
            var comboBox = this.Controls.Find("comboBoxClients", true).FirstOrDefault() as ComboBox;
            if (comboBox == null) return;

            comboBox.DataSource = _instancia_clientes.Lista_clientes_UI;
            comboBox.DisplayMember = "Nombre";
        }

        private void PopulateNodeGrid()
        {
            var dgv = this.Controls.Find("dgvNodes", true).FirstOrDefault() as DataGridView;
            var comboBox = this.Controls.Find("comboBoxClients", true).FirstOrDefault() as ComboBox;
            if (dgv == null || comboBox == null) return;

            dgv.Rows.Clear();
            var selectedClient = comboBox.SelectedItem as Cliente;
            if (selectedClient == null) return;

            foreach (var nodo in selectedClient.Nodos)
            {
                int rowIndex = dgv.Rows.Add(nodo.Nodo_nombre, nodo.Valor?.ToString() ?? "(esperando...)", "");
                dgv.Rows[rowIndex].Tag = nodo;
            }
        }

        private async Task DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null || e.RowIndex < 0 || e.ColumnIndex != dgv.Columns["WriteButton"].Index) return;

            var row = dgv.Rows[e.RowIndex];
            var nodo = row.Tag as Nodo;

            var comboBox = this.Controls.Find("comboBoxClients", true).FirstOrDefault() as ComboBox;
            var cliente = comboBox?.SelectedItem as Cliente;

            var valorAEscribir = row.Cells["WriteValue"].Value?.ToString();

            if (nodo == null || cliente == null || string.IsNullOrEmpty(valorAEscribir))
            {
                MessageBox.Show("Por favor, introduce un valor para escribir.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bool exito = await _instancia_clientes.EscribirValorNodoAsync(cliente.Nombre, nodo.Nodo_id, valorAEscribir);
                if (exito)
                {
                    MessageBox.Show($"Valor '{valorAEscribir}' escrito correctamente en el nodo '{nodo.Nodo_nombre}'.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No se pudo escribir el valor. Compruebe la conexión o los permisos.", "Fallo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al escribir el valor: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Instancia_clientes_actualizar_ui(object? sender, Nodo updatedNode)
        {
            // --- SOLUCIÓN AL ERROR DE CIERRE ---
            // 1. Comprobamos si el formulario se está destruyendo o si su "handle" no ha sido creado.
            // Esto evita que se intente actualizar un control que ya no existe.
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                return;
            }

            // 2. Comprobamos si la llamada viene de un hilo de fondo.
            if (this.InvokeRequired)
            {
                try
                {
                    // 3. Usamos Invoke para ejecutar la actualización en el hilo de la UI.
                    // Lo envolvemos en un try-catch por si el formulario se destruye justo
                    // entre la comprobación y la ejecución del Invoke.
                    this.Invoke(new Action(() => Instancia_clientes_actualizar_ui(sender, updatedNode)));
                }
                catch (ObjectDisposedException)
                {
                    // Es seguro ignorar este error, ya que solo significa que el formulario se cerró.
                }
                return;
            }

            // --- Lógica de actualización (sin cambios) ---
            var dgv = this.Controls.Find("dgvNodes", true).FirstOrDefault() as DataGridView;
            var comboBox = this.Controls.Find("comboBoxClients", true).FirstOrDefault() as ComboBox;
            if (dgv == null || comboBox == null || comboBox.SelectedItem == null) return;

            var selectedClient = comboBox.SelectedItem as Cliente;
            if (selectedClient != null && selectedClient.Nodos.Any(n => n.Nodo_id == updatedNode.Nodo_id))
            {
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.Tag is Nodo nodoUI && nodoUI.Nodo_id == updatedNode.Nodo_id)
                    {
                        row.Cells["ReadValue"].Value = updatedNode.Valor?.ToString() ?? "(null)";
                        break;
                    }
                }
            }
        }
    }
}
