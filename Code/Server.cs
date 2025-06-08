using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPCplug.Code
{
    internal class Server
    {
        public async Task async_server()
        {
            await Task.Delay(3000);
            MessageBox.Show("Servidor iniciado...", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }
    }
}
