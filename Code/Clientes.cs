using Microsoft.VisualBasic.ApplicationServices;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.Design.AxImporter;



namespace OPCplug.Code
{
    //Objeto cliente para almacenar toda la info de los clientes que se conecta
    public class Cliente
    {
        public string? Nombre { get; set; }
        public string? EndpointURL { get; set; }
        public List<Nodo> Nodos { get; set; } = [];
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }
    //Objeto Nodo, para hacer una lista de nodos en los clientes
    public class Nodo
    {
        public string? Nodo_id { get; set; }
        public string? Nodo_nombre { get; set; }
        public object? Valor { get; set; }
    }
    //Clase que se inicializa, donde se almacena todo, es el objeto que guarda todos los datos
    //almacena la listas de clientes y datos de conexion, cuando se cierra la conexion se elimina
    public class Clientes
    {
        private static CancellationTokenSource? _generador_Token;

        //EVENTOS DE HILO
        public event EventHandler? Arrancar_Clientes;
        public event EventHandler<Exception>? Error_Clientes;
        public event EventHandler? Parar_Clientes;

        //EVENTO DE ACTUALIZAR LA UI CLIENTES
        public event EventHandler<Nodo> Actualizar_valor_ui;

        private readonly List<Cliente> _lista_clientes_lectura = [];
        private readonly List<Cliente> _lista_clientes_UI = [];
        private readonly ConcurrentDictionary<string, Session> _sesionesActivas = new();
        //Listas que se envian al form/UI, para no leer o escribir en las listas verdaderas y cagarla y crashear el programa
        public IReadOnlyList<Cliente> Lista_clientes_lectura => _lista_clientes_lectura;
        public IReadOnlyList<Cliente> Lista_clientes_UI => _lista_clientes_UI;

        //Constructor publico de Clientes, cuando se inicializa se le llama para cargar la lista de clientes del XML
        public Clientes()
        {
            Configurar_clientes();
        }
        //Metodo principal, maneja el token para cerrar las task
        //Hace una lista de clientes con sus task, e inicializa una task por cliente
        public async Task Async_clientes()
        {
            _generador_Token = new CancellationTokenSource();
            CancellationToken token_cancelacion = _generador_Token.Token;

            try
            {
                //Generamos una lista de clientes donde almacenaremos las intancias/task de los clientes
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
                //No deberia ser necesario limpiar las listas, tengo dudas de esto porque en el form cuando cierras la lectura, se elimina la instancia de clientes
                //Pero a la vez el consumo de memoria si no pones el clear aqui se va sumando con cada instancia...imagino que llegado un limite el coletor de basura
                //de C# actuara y lo borrara automaticamente, prefiero limpiarlo yo
                _lista_clientes_lectura.Clear();
                _lista_clientes_UI.Clear();
                Parar_Clientes?.Invoke(this, EventArgs.Empty);
                _generador_Token?.Dispose();
                _generador_Token = null;
                
                

            }
        }

        // ⚠️ Atención, cuidado...
        //
        // Agarrate, lo que estás a punto de presenciar es el resultado de aproximadamente seis horas
        // de darme cabezazos contra un muro en una habitación oscura.
        //
        // Esta función está bien. Funciona bien. Incluso diría que con la parte de
        // monitorización es casi *óptima*. Pero llegar a este punto ha sido de todo menos
        // programar, a sido mas loteria, y prueba y error de como ostias funciona la sesion y los nodos.
        //
        // La documentación de OPC UA y Siemens es críptica o nula.
        //
        // Francamente, preferiría colgarme por las extremidades más sensibles de mi anatomía
        // con una soga antes que volver a escribir esta función desde cero.
        //
        // Así que si vas a tocar algo aquí... suerte... titan...te va ha hacer falta...
        //
        private async Task Async_lectura_cliente(Cliente cliente, CancellationToken token)
        {
            //Generamos el archivo de configuracion
            var configuracion = new Opc.Ua.ApplicationConfiguration
            {
                //Identificadores de la conexion
                ApplicationName = $"OPCplug_Client_{cliente.Nombre}",
                ApplicationType = ApplicationType.Client,
                //Configuramos la seguridad, y donde se almacena los certificados
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
                    //Aqui el unico que importa o hace falta es AutoAcceptUntrustedCertificates, en true no hace la conexion sin seguridad 
                    //ni manda certificados ni nada, esto se toca dependiendo del servidor, como dije en el server, mejor dar esta seguridad 
                    //con un firewall bien configurado
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true,
                    RejectSHA1SignedCertificates = false,
                    MinimumCertificateKeySize = 1024
                },
                //Mas configuraciones, que si una conexion no responde por 15 s se anula o si la sesion se pierde por 60 desconecto la sesion y hay que volver a crearla
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };
            //Validamos la configuracion, como en el server, mejor que pete aqui que cuando se crea la sub-task de lectura de cliente
            await configuracion.Validate(ApplicationType.Client);
            //Igual que el server, se genera una aplicacion, para poder ir comprobandola, y checkeamos el certificado
            var application = new ApplicationInstance(configuracion);
            if (!await application.CheckApplicationInstanceCertificate(true, 2048))
                throw new Exception($"No se pudo crear el certificado para el cliente {cliente.Nombre}.");
            //Bool auxiliar, si el keepalive se pierde, lo activamos para cerrar la sesion y volver a crearla, esto se debe a que si el server se reinicia
            //la sesion se pierde y ya no vale, es necesario cerrarla y vovler a generar otra.
            bool reconectar;
            do
            {
                reconectar = false;
                Session? sesion = null;
                try
                {
                    //generamos la URL donde hay que conectarse
                    //El usuario y contraseña
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
                    //Ensamblamos todas las configuraciones en una y generamos la sesion
                    var configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription, EndpointConfiguration.Create(configuracion));

                    sesion = await Session.Create(configuracion, configuredEndpoint, false, "", 60000, userIdentity, null);

                    _sesionesActivas[cliente.Nombre] = sesion;

                    // Detectar desconexiones con KeepAlive, emmm.... lo detecta... funciona... ok....
                    //Esto del keepalive al igual que todo, 0 documentacion, a sido 1 foro de stackoverflow y chatgpt, que diciendole la idea, me ha soltado esto
                    sesion.KeepAlive += (s, e) =>
                    {
                        if (ServiceResult.IsBad(e.Status))
                        {
                            Debug.WriteLine($"[{cliente.Nombre}] KeepAlive Fallido: {e.Status}");
                            reconectar = true;
                            _sesionesActivas.TryRemove(cliente.Nombre, out _);
                            sesion?.Close();
                        }
                    };
                    //Vale aqui esta la gracia de la funcion, y la diferencia con usar:
                    //
                    //      var nodo = new NodeId(3, 4); Add commentMore actions
                    //      var valor = await _sesion_OPC.ReadValueAsync(nodo);
                    //
                    //En vez de tener un bucle while que cada 10ms leemos los valores, y depende la cantidad de valores pues si no lo haces con hilos o haces threats
                    //con slides de los nodos que quieres leer, lo que hacemos es decir oye queriero que me avises si alguno de estas variables cambia
                    //entonces te suscribes al server y le indicas que monitorice las variables, y el server cuando esta cambia, te envia una flag como que ha variado
                    //y es entonces cuando actualizas el valor de la lectura, esto permite tener umbrales y solamente leer si varia x cantidad, supuestamente es mas optimo
                    //no lo se, soy programador no ingeniero, es como hay que hacerlo en OPC pues es como se hace

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
                    _sesionesActivas.TryRemove(cliente.Nombre, out _);
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


        public async Task<bool> EscribirValorNodoAsync(string nombreCliente, string nodeId, string valor)
        {
            if (_sesionesActivas.TryGetValue(nombreCliente, out Session sesion) && sesion.Connected)
            {
                try
                {
                    var nodoAEscribir = new NodeId(nodeId);

                    // --- CÓDIGO CORREGIDO PARA OBTENER EL TIPO DE DATO ---
                    // 1. Preparamos una petición para leer solo el atributo DataType.
                    ReadValueIdCollection nodesToRead = new()
                    {
                        new ReadValueId()
                        {
                            NodeId = nodoAEscribir,
                            AttributeId = Opc.Ua.Attributes.DataType // Usamos la enumeración correcta
                        }
                    };

                    // 2. Ejecutamos la lectura.
                    sesion.Read(
                        null,
                        0,
                        TimestampsToReturn.Neither,
                        nodesToRead,
                        out DataValueCollection results,
                        out DiagnosticInfoCollection _);

                    // 3. Comprobamos el resultado.
                    if (StatusCode.IsBad(results[0].StatusCode))
                    {
                        throw new ServiceResultException((uint)results[0].StatusCode, "No se pudo leer el tipo de dato del nodo.");
                    }

                    // 4. Extraemos el tipo de dato y lo convertimos.
                    var dataTypeId = results[0].Value as NodeId;
                    var builtInType = TypeInfo.GetBuiltInType(dataTypeId, sesion.SystemContext.TypeTable);
                    var valorConvertido = TypeInfo.Cast(valor, builtInType);

                    // 5. Preparamos y ejecutamos la escritura.
                    WriteValueCollection valoresAEscribir = new()
                    {
                        new WriteValue()
                        {
                            NodeId = nodoAEscribir,
                            AttributeId = Opc.Ua.Attributes.Value,
                            Value = new DataValue(new Variant(valorConvertido))
                        }
                    };

                    // Usamos la versión síncrona dentro de la tarea asíncrona, es más simple para este caso.
                    sesion.Write(null, valoresAEscribir, out StatusCodeCollection writeResults, out _);

                    if (StatusCode.IsGood(writeResults[0]))
                    {
                        Debug.WriteLine($"Escritura exitosa en {nodeId}.");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"Falló la escritura en {nodeId} con estado: {writeResults[0]}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al escribir en {nodeId}: {ex.Message}");
                    return false;
                }
            }

            Debug.WriteLine($"No se encontró una sesión activa para el cliente '{nombreCliente}'.");
            return false;
        }

        //Funcion que si el server notifica que la variable a cambiado, satla este evento y cambia el valor
        //Como he dicho antes, esta mejor porque no cuelgas un hilo en un bucle spamenado la lectura.
        //Te suscribes a un evento y cuando este salta, lanza la funcion
        private void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            if (e.NotificationValue is MonitoredItemNotification notification && item.Handle is Nodo nodoAsociado)
            {
                // 1. Actualiza el valor en nuestra lista interna.
                nodoAsociado.Valor = notification.Value.Value;

                // 2. DISPARA EL EVENTO para notificar a la UI que este nodo específico ha cambiado.
                Actualizar_valor_ui?.Invoke(this, nodoAsociado);
            }
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
            if (_generador_Token != null && !_generador_Token.IsCancellationRequested)
            {
                Debug.WriteLine("Recibida solicitud para detener la lectura de clientes...");
                _generador_Token.Cancel();
            }
        }














    }
}

