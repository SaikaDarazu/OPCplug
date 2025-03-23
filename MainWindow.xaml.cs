using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Opc.Ua.Client;
using System.Threading;
using Opc.Ua;



namespace OPCplug
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
        {

        private Session _sesion_OPC;
        private CancellationTokenSource _cancelador_lectura;
        private bool _lectura_activa = false;



        public MainWindow()
            {
            InitializeComponent();
            }

        private async void Lectura_Clientes(object sender, RoutedEventArgs e)
        {
            if (!_lectura_activa)
            {
                // Activar lectura
                _lectura_activa = true;
                _cancelador_lectura = new CancellationTokenSource();

                try
                {
                    _sesion_OPC = await Crear_Cliente_Lectura.Crear_Cliente_Lectura_Async("opc.tcp://192.168.1.123:4840",
                        usuario: "Saika", contraseña: "Xispas0670999823", usar_seguridad: false);
                   
                    _ = Task.Run(async () =>
                    {
                        while (!_cancelador_lectura.Token.IsCancellationRequested)
                        {
                            try
                            {
                                // Leer el valor del nodo
                                var nodo = new NodeId(3,4);
                                var valor = await _sesion_OPC.ReadValueAsync(nodo);

                                // Actualizar UI desde el hilo principal
                                Dispatcher.Invoke(() =>
                                {
                                    ReadOnlyField.Text = valor.Value?.ToString() ?? "Valor nulo";
                                });
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ReadOnlyField.Text = $"Error: {ex.Message}";
                                });
                            }

                            await Task.Delay(500, _cancelador_lectura.Token); // Esperar 0.5 segundos
                        }
                    }, _cancelador_lectura.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}\n\nDetalle: {ex.InnerException?.Message}");
                    _lectura_activa = false;
                }
            }
            else
            {
                // Detener lectura
                _cancelador_lectura.Cancel();
                _sesion_OPC?.Close();
                _sesion_OPC?.Dispose();
                _sesion_OPC = null;
                _lectura_activa = false;
                ReadOnlyField.Text = "Lectura detenida";
            }
        }
    }
}