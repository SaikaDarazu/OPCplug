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

        private void run_server_click(object sender, EventArgs e)
        {
            if (servidor != null)
            {
                 MessageBox.Show
                    (
                         $"El servidor ya está en ejecución.",
                         $"Servidor ID: {servidor?.Id}",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Information
                    );
                return;
            }
            servidor = new Server();
            Task.Run(() => servidor.async_server());
        }
    }
}
