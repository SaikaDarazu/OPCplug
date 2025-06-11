using OPCplug.Code;
using System.Text;
using System.Threading.Tasks;

namespace OPCplug
{
    public partial class Form1 : Form
    {
        Server? servidor;
        Form? ventana_clientes;
        Clientes clientes;

        private System.Windows.Forms.Timer? uiUpdateTimer;
        private RichTextBox? infoBoxClientes;

        public Form1()
        {
            InitializeComponent();
            this.MinimumSize = new Size(800, 600);
            this.Size = new Size(800, 600);
            this.Text = "OPCplug - Servidor OPC UA";

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            servidor?.Stop();
            clientes?.Stop();
        }

        private void Run_server_click(object sender, EventArgs e)
        {
            this.Run_server.Enabled = false;

            if (servidor != null)
            {
                // Paramos el server
                this.Run_server.Text = "Deteniendo...";
                this.Run_server.Refresh();
                Task.Run(() => servidor.Stop());
            }
            else
            {
                // arrancamos el server
                this.Run_server.Text = "Arrancando...";
                servidor = new Server();

                // Eventos para escuchar lo que hace el server en su Task
                servidor.Arrancar_Servidor += Servidor_Arrancar_Servidor;
                servidor.Parar_Servidor += Servidor_Parar_Servidor;
                servidor.Error_Servidor += Error_Servidor;

                //Esto aunque ya ocurre en el hilo del servidor, lo hacemos en un Task otra vez para evitar bloqueos
                //Porque ha veces petaba el hilo del server y no ejecutaba correctamente el reseteo del botón.

                Task.Run(() =>
                {
                    try
                    {
                        servidor.Async_server();
                    }
                    catch (Exception ex)
                    {
                        Error_Servidor(this, ex);
                    }
                });

            }
        }

        private void Run_clientes_Click(object sender, EventArgs e)
        {
            this.Run_clientes.Enabled = false;

            if (clientes != null)
            {
                // Paramos el server
                this.Run_clientes.Text = "Deteniendo...";
                this.Run_clientes.Refresh();
                Task.Run(() => clientes.Stop());
            }
            else
            {
                // arrancamos el server
                this.Run_clientes.Text = "Arrancando...";
                clientes = new Clientes();

                ventana_clientes = MostrarInformacionClientes(clientes);
                if (ventana_clientes != null)
                {
                    ventana_clientes.FormClosing += Cerrar_ventana_clientes;
                }

                
                uiUpdateTimer = new System.Windows.Forms.Timer();
                uiUpdateTimer.Interval = 100; // Refrescar cada segundo
                uiUpdateTimer.Tick += UpdateClientInfoWindow;
                uiUpdateTimer.Start();

                // Eventos para escuchar lo que hace el server en su Task
                clientes.Arrancar_Clientes += Clientes_Arrancar;
                clientes.Parar_Clientes += Clientes_Parar;
                clientes.Error_Clientes += Error_Clientes;

                //Esto aunque ya ocurre en el hilo del servidor, lo hacemos en un Task otra vez para evitar bloqueos
                //Porque ha veces petaba el hilo del server y no ejecutaba correctamente el reseteo del botón.

                Task.Run(() =>
                {
                    try
                    {
                        clientes.Async_clientes();
                    }
                    catch (Exception ex)
                    {
                        Error_Clientes(this, ex);
                    }
                });

            }
        }

        private void UpdateClientInfoWindow(object? sender, EventArgs e)
        {
            // Si el gestor de clientes o la caja de texto no existen, no hacemos nada
            if (clientes == null || infoBoxClientes == null || infoBoxClientes.IsDisposed)
            {
                uiUpdateTimer?.Stop();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Se han cargado {clientes.Lista_clientes_UI.Count} clientes (actualizado en tiempo real):\n");

            // Usamos la Lista_clientes_UI porque es la que contiene la copia de los datos
            // que se actualiza en el evento OnNotification
            foreach (var cliente in clientes.Lista_clientes_lectura) // Leemos la lista de lectura que tiene los datos reales
            {
                sb.AppendLine($"--- Cliente: {cliente.Nombre} ---");
                sb.AppendLine($"URL: {cliente.EndpointURL}");
                sb.AppendLine("Nodos y Valores:");
                if (cliente.Nodos.Any())
                {
                    foreach (var nodo in cliente.Nodos)
                    {
                        // Mostramos el valor del nodo. Si es null, mostramos "(esperando...)"
                        string valorMostrado = nodo.Valor?.ToString() ?? "(esperando...)";
                        sb.AppendLine($"  - {nodo.Nodo_nombre}: {valorMostrado}  (ID: {nodo.Nodo_id})");
                    }
                }
                else { sb.AppendLine("  (No se han definido nodos)"); }
                sb.AppendLine();
            }

            // Actualizamos el texto en el hilo de la UI
            if (infoBoxClientes.IsHandleCreated)
            {
                infoBoxClientes.Invoke((MethodInvoker)delegate {
                    infoBoxClientes.Text = sb.ToString();
                });
            }
        }

        private Form MostrarInformacionClientes(Clientes clientesManager)
        {
            Form infoForm = new Form { /* ... config ... */ };

            // Creamos el RichTextBox y lo guardamos en nuestra variable de clase
            infoBoxClientes = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                WordWrap = false
            };

            infoForm.Controls.Add(infoBoxClientes);

            // Realizamos la primera actualización de la UI
            UpdateClientInfoWindow(null, EventArgs.Empty);

            infoForm.Show();
            return infoForm;
        }
        private void Cerrar_ventana_clientes(object? sender, FormClosingEventArgs e)
        {
            clientes?.Stop();
        }
        // --- EVENTOS SERVER ---
        // Aviso de que el server ha arrancado
        private void Servidor_Arrancar_Servidor(object? sender, EventArgs e)
        {
            // Usamos Invoke para asegurarnos de que el código se ejecuta en el hilo de la UI.
            this.Invoke((MethodInvoker)delegate
            {
                this.Run_server.Text = "Detener Servidor";
                this.Run_server.BackColor = Color.Salmon;
                this.Run_server.Enabled = true; // Reactiva el botón.
            });
        }
        // Aviso de que el server ha parado
        private void Servidor_Parar_Servidor(object? sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                // eliminamos los eventos para evitar fugas de memoria.
                if (servidor != null)
                {
                    servidor.Arrancar_Servidor -= Servidor_Arrancar_Servidor;
                    servidor.Parar_Servidor -= Servidor_Parar_Servidor;
                    servidor.Error_Servidor -= Error_Servidor;
                }

                servidor = null; // Marca el servidor como detenido.
                this.Run_server.Text = "Arrancar Servidor";
                this.Run_server.BackColor = Color.LightGreen;
                this.Run_server.Enabled = true; // Reactiva el botón.
            });
        }
        // Aviso de que el server ha petado
        private void Error_Servidor(object? sender, Exception ex)
        {
            this.Invoke((MethodInvoker)delegate
            {
                MessageBox.Show($"Error crítico al iniciar el servidor:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                if (servidor != null)
                {
                    servidor.Arrancar_Servidor -= Servidor_Arrancar_Servidor;
                    servidor.Parar_Servidor -= Servidor_Parar_Servidor;
                    servidor.Error_Servidor -= Error_Servidor;
                    servidor = null;
                }

                this.Run_server.Text = "Arrancar Servidor";
                this.Run_server.BackColor = Color.LightGreen;
                this.Run_server.Enabled = true;
            });
        }

        // --- EVENTOS CLIENTE ---
        private void Clientes_Arrancar(object? sender, EventArgs e)
        {
            // Usamos Invoke para asegurarnos de que el código se ejecuta en el hilo de la UI.
            this.Invoke((MethodInvoker)delegate
            {
                this.Run_clientes.Text = "Detener Lectura Clientes";
                this.Run_clientes.BackColor = Color.Salmon;
                this.Run_clientes.Enabled = true; // Reactiva el botón.
            });
        }
        // Aviso de que el server ha parado
        private void Clientes_Parar(object? sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                // eliminamos los eventos para evitar fugas de memoria.
                if (clientes != null)
                {
                    clientes.Arrancar_Clientes -= Clientes_Arrancar;
                    clientes.Parar_Clientes -= Clientes_Parar;
                    clientes.Error_Clientes -= Error_Clientes;
                }
                if (ventana_clientes != null && !ventana_clientes.IsDisposed)
                {
                    // Nos desuscribimos del evento para evitar que se llame a Stop() de nuevo.
                    ventana_clientes.FormClosing -= Cerrar_ventana_clientes;
                    ventana_clientes.Close();
                }
                clientes = null; // Marca el servidor como detenido.
                this.Run_clientes.Text = "Arrancar Lectura Clientes";
                this.Run_clientes.BackColor = Color.LightGreen;
                this.Run_clientes.Enabled = true; // Reactiva el botón.
            });
        }
        // Aviso de que el server ha petado
        private void Error_Clientes(object? sender, Exception ex)
        {
            this.Invoke((MethodInvoker)delegate
            {
                MessageBox.Show($"Error crítico al iniciar el lectura clientes:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                if (clientes != null)
                {
                    clientes.Arrancar_Clientes -= Clientes_Arrancar;
                    clientes.Parar_Clientes -= Clientes_Parar;
                    clientes.Error_Clientes -= Error_Clientes;
                }
                if (ventana_clientes != null && !ventana_clientes.IsDisposed)
                {
                    // Nos desuscribimos del evento para evitar que se llame a Stop() de nuevo.
                    ventana_clientes.FormClosing -= Cerrar_ventana_clientes;
                    ventana_clientes.Close();
                }
                clientes = null;
                this.Run_clientes.Text = "Arrancar Lectura Clientes";
                this.Run_clientes.BackColor = Color.LightGreen;
                this.Run_clientes.Enabled = true;
            });
        }


    }
}
