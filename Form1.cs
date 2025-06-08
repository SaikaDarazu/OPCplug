using OPCplug.Code;
using System.Threading.Tasks;

namespace OPCplug
{
    public partial class Form1 : Form
    {
        Server? servidor;
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
        }

        private void run_server_click(object sender, EventArgs e)
        {
            this.run_server.Enabled = false;

            if (servidor != null)
            {
                // El servidor está en marcha, así que lo detenemos.
                this.run_server.Text = "Deteniendo...";
                this.run_server.Refresh();
                Task.Run(() => servidor.Stop());
            }
            else
            {
                // El servidor está detenido, así que lo iniciamos.
                this.run_server.Text = "Arrancando...";
                servidor = new OPCplug.Code.Server();

                // Eventos
                servidor.Arrancar_Servidor += Servidor_Arrancar_Servidor;
                servidor.Parar_Servidor += Servidor_Parar_Servidor;
                servidor.Error_Servidor += Error_Servidor;

                Task.Run(() =>
                {
                    try
                    {
                        servidor.async_server();
                    }
                    catch (Exception ex)
                    {
                        Error_Servidor(this, ex);
                    }
                });

            }
        }

        // --- MANEJADORES DE EVENTOS ---

        // Este método se ejecutará CUANDO el servidor arranque con éxito.
        private void Servidor_Arrancar_Servidor(object? sender, EventArgs e)
        {
            // Usamos Invoke para asegurarnos de que el código se ejecuta en el hilo de la UI.
            this.Invoke((MethodInvoker)delegate {
                this.run_server.Text = "Detener Servidor";
                this.run_server.BackColor = Color.Salmon;
                this.run_server.Enabled = true; // Reactiva el botón.
            });
        }

        // Este método se ejecutará CUANDO la tarea del servidor se detenga (ya sea por éxito o por error).
        private void Servidor_Parar_Servidor(object? sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)delegate {
                // Desuscribe los eventos para evitar fugas de memoria.
                if (servidor != null)
                {
                    servidor.Arrancar_Servidor -= Servidor_Arrancar_Servidor;
                    servidor.Parar_Servidor -= Servidor_Parar_Servidor;
                    servidor.Error_Servidor -= Error_Servidor;
                }

                servidor = null; // Marca el servidor como detenido.
                this.run_server.Text = "Arrancar Servidor";
                this.run_server.BackColor = Color.LightGreen;
                this.run_server.Enabled = true; // Reactiva el botón.
            });
        }

        // Este método se ejecutará SI el servidor falla al arrancar.
        private void Error_Servidor(object? sender, Exception ex)
        {
            this.Invoke((MethodInvoker)delegate {
                MessageBox.Show($"Error crítico al iniciar el servidor:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                if (servidor != null)
                {
                    servidor.Arrancar_Servidor -= Servidor_Arrancar_Servidor;
                    servidor.Parar_Servidor -= Servidor_Parar_Servidor;
                    servidor.Error_Servidor -= Error_Servidor;
                    servidor = null;
                }

                this.run_server.Text = "Arrancar Servidor";
                this.run_server.BackColor = Color.LightGreen;
                this.run_server.Enabled = true;
            });
        }










    }
}
