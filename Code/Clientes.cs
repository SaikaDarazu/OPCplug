using Microsoft.VisualBasic.ApplicationServices;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.Design.AxImporter;



namespace OPCplug.Code
{
    public class Cliente
    {
        public string? Nombre { get; set; }
        public string? EndpointURL { get; set; }
        public List<Nodo> Nodos { get; set; } = [];
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

    public class Nodo
    {
        public string? Nodo_id { get; set; }
        public string? Nodo_nombre { get; set; }
        public object? Valor { get; set; }
    }

    public class Clientes
    {
        private static CancellationTokenSource? _generador_Token;

        public event EventHandler? Arrancar_Clientes;
        public event EventHandler<Exception>? Error_Clientes;
        public event EventHandler? Parar_Clientes;

        private readonly List<Cliente> _lista_clientes_lectura = [];
        private readonly List<Cliente> _lista_clientes_UI = [];

        public IReadOnlyList<Cliente> Lista_clientes_lectura => _lista_clientes_lectura;
        public IReadOnlyList<Cliente> Lista_clientes_UI => _lista_clientes_UI;

        public Clientes()
        {
            Configurar_clientes();
        }

        public async Task Async_clientes()
        {
            _generador_Token = new CancellationTokenSource();
            CancellationToken token_cancelacion = _generador_Token.Token;

            try
            {
                Arrancar_Clientes?.Invoke(this, EventArgs.Empty);
                var lista_task_clientes = new List<Task>();

                foreach (var cliente in _lista_clientes_lectura)
                {
                    lista_task_clientes.Add(Task.Run(() => Async_lectura_cliente(cliente, token_cancelacion), token_cancelacion));

                }

                await Task.Delay(Timeout.Infinite, token_cancelacion);
            }
            catch (TaskCanceledException)
            {
                 Debug.WriteLine("La lectura de clientes se ha detenido correctamente.");
                Stop();
            }
            catch (Exception ex)
            {
                Error_Clientes?.Invoke(this, ex);
                 Debug.WriteLine($"Error en el proceso principal de clientes: {ex.Message}");
                string errorMsg = $"Error al iniciar:\n{ex.Message}\n\n{(ex.InnerException != null ? ex.InnerException.Message : "")}";
                MessageBox.Show(errorMsg, "Error de Servidor OPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _lista_clientes_lectura.Clear();
                _lista_clientes_UI.Clear();
                Parar_Clientes?.Invoke(this, EventArgs.Empty);
                _generador_Token?.Dispose();
                _generador_Token = null;
                
                

            }
        }

        private async Task Async_lectura_cliente(Cliente cliente, CancellationToken token)
        {
            var configuracion = new Opc.Ua.ApplicationConfiguration
            {
                ApplicationName = $"OPCplug_Client_{cliente.Nombre}",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = @"%ProgramData%\OPC Foundation\CertificateStores\MachineDefault",
                        SubjectName = $"CN={System.Net.Dns.GetHostName()}, DC=OPC Foundation"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = @"%ProgramData%\OPC Foundation\CertificateStores\UA Applications",
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = @"%ProgramData%\OPC Foundation\CertificateStores\UA Certificate Authorities",
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = @"%ProgramData%\OPC Foundation\CertificateStores\RejectedCertificates",
                    },
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true,
                    RejectSHA1SignedCertificates = false,
                    MinimumCertificateKeySize = 1024
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };

            await configuracion.Validate(ApplicationType.Client);

            var application = new ApplicationInstance(configuracion);
            if (!await application.CheckApplicationInstanceCertificate(true, 2048))
                throw new Exception($"No se pudo crear el certificado para el cliente {cliente.Nombre}.");

            bool reconectar;
            do
            {
                reconectar = false;
                Session? sesion = null;
                try
                {
                    var endpointDescription = CoreClientUtils.SelectEndpoint(cliente.EndpointURL, false);
                    IUserIdentity? userIdentity;
                    try
                    {
                        userIdentity = (string.IsNullOrWhiteSpace(cliente.UserName) || string.IsNullOrWhiteSpace(cliente.Password))
                            ? new UserIdentity(new AnonymousIdentityToken())
                            : new UserIdentity(cliente.UserName, cliente.Password);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error creando la identidad de usuario para la sesión OPC.", ex);
                    }

                var configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription, EndpointConfiguration.Create(configuracion));

                    sesion = await Session.Create(configuracion, configuredEndpoint, false, "", 60000, userIdentity, null);

                    // Detectar desconexiones con KeepAlive
                    sesion.KeepAlive += (s, e) =>
                    {
                        if (ServiceResult.IsBad(e.Status))
                        {
                            Debug.WriteLine($"[{cliente.Nombre}] KeepAlive Fallido: {e.Status}");
                            reconectar = true;
                            sesion?.Close();
                        }
                    };

                    var subscripcion = new Subscription(sesion.DefaultSubscription) { PublishingInterval = 200 };
                    var listaItems = new List<MonitoredItem>();
                    foreach (var nodo in cliente.Nodos)
                    {
                        var monitoredItem = new MonitoredItem(subscripcion.DefaultItem)
                        {
                            DisplayName = nodo.Nodo_nombre,
                            StartNodeId = nodo.Nodo_id,
                            Handle = nodo,
                            SamplingInterval = 100
                        };
                        monitoredItem.Notification += OnNotification;
                        listaItems.Add(monitoredItem);
                    }
                    subscripcion.AddItems(listaItems);
                    sesion.AddSubscription(subscripcion);

                    subscripcion.Create();
                    subscripcion.ApplyChanges();
                    Debug.WriteLine($"[{cliente.Nombre}] Conectado y suscrito. Monitoreando cambios...");

                    // Espera mientras no haya reconexión ni cancelación
                    while (!reconectar && !token.IsCancellationRequested)
                    {
                        await Task.Delay(1000, token);
                    }
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{cliente.Nombre}] Error: {ex.Message}");
                    reconectar = true;
                }
                finally
                {
                    sesion?.Close();
                    sesion?.Dispose();
                }

                if (reconectar && !token.IsCancellationRequested)
                {
                    Debug.WriteLine($"[{cliente.Nombre}] Reintentando conexión en 15 segundos...");
                    try { await Task.Delay(15000, token); }
                    catch (TaskCanceledException) { break; }
                }

            } while (!token.IsCancellationRequested);
        }

        private static void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            var notification = e.NotificationValue as MonitoredItemNotification;
            if (notification == null) return;

            var nodoAsociado = item.Handle as Nodo;
            if (nodoAsociado == null) return;

            nodoAsociado.Valor = notification.Value.Value;
        }

        private void Configurar_clientes()
        {
            try
            {
                _lista_clientes_lectura.Clear();
                _lista_clientes_UI.Clear();

                string filePath = LeerXML.get_ruta_xml_datos_clientes();
                if (!File.Exists(filePath)) return;

                XDocument doc = XDocument.Load(filePath);

                foreach (var clientElement in doc.Descendants("Client"))
                {
                    var userIdentityElement = clientElement.Element("UserIdentity");

                    var clienteOriginal = new Cliente
                    {
                        Nombre = clientElement.Attribute("name")?.Value ?? "SinNombre",
                        EndpointURL = clientElement.Element("EndpointUrl")?.Value,
                        UserName = userIdentityElement?.Element("UserName")?.Value,
                        Password = userIdentityElement?.Element("Password")?.Value
                    };

                    var leer_nodos = clientElement.Descendants("Node").Select(nodeElement => new Nodo
                    {
                        Nodo_id = nodeElement.Attribute("nodeId")?.Value,
                        Nodo_nombre = nodeElement.Attribute("displayName")?.Value
                    }).ToList();

                    clienteOriginal.Nodos.AddRange(leer_nodos);
                    _lista_clientes_lectura.Add(clienteOriginal);

                    // Crea la copia profunda para la UI.
                    var copiaCliente = new Cliente
                    {
                        Nombre = clienteOriginal.Nombre,
                    };
                    foreach (var nodoOriginal in clienteOriginal.Nodos)
                    {
                        copiaCliente.Nodos.Add(new Nodo
                        {
                            Nodo_id = nodoOriginal.Nodo_id,
                            Nodo_nombre = nodoOriginal.Nodo_nombre,
                            Valor = nodoOriginal.Valor
                        });
                    }
                    _lista_clientes_UI.Add(copiaCliente);
                }
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"Error fatal al leer el XML de clientes: {ex.Message}");
            }
        }

        public void Stop()
        {
            _generador_Token?.Cancel();
        }














    }
}

