﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.VisualBasic.ApplicationServices;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System.Drawing;
using System.Threading;


namespace OPCplug.Code
{
    internal class Server
    {
        private static string? _ApplicationName;
        private static string? _ApplicationUri;
        private static string? _ProductUri;
        private static string? _ServerUrl;
        private static bool _AutoAcceptUntrustedCertificates;
        public string Id { get; } = Guid.NewGuid().ToString();

        private CancellationTokenSource? _generador_Token;
        private ApplicationInstance? _aplicacion;

        
        
        public event EventHandler? Arrancar_Servidor;
        public event EventHandler<Exception>? Error_Servidor;
        public event EventHandler? Parar_Servidor;




        public async Task Async_server()
        {

            //Generamos el token de cancelacion de Task
            _generador_Token = new CancellationTokenSource();
            CancellationToken token_cancelacion = _generador_Token.Token;

            try
            {
                
                //LLamo a mi metodo para cargar en las variables staticas todos los datos que necesitamos para configurar el servidor
                Configurar_parametros_servidor();
               
            
                //Generamos el archivo de configuracion vacio
                var configuracion = new Opc.Ua.ApplicationConfiguration();


                //Identificadores de la aplicacion, algo asi como el numero de serie o mac
                configuracion.ApplicationUri = Server._ApplicationUri;
                configuracion.ProductUri = Server._ProductUri;
                configuracion.ApplicationName = Server._ApplicationName;
                configuracion.ApplicationType = ApplicationType.Server;


                //Configuramos la conexion IP
                // Configuramos la politica de encriptamiento que "puede", repito "PUEDE" usar el server: nada, Firmado y Firmado y encriptado
                configuracion.ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = { Server._ServerUrl },
                    SecurityPolicies =
                    {
                    new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None },
                    new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.Sign, SecurityPolicyUri = SecurityPolicies.Basic256Sha256 },
                    new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.SignAndEncrypt, SecurityPolicyUri = SecurityPolicies.Basic256Sha256 }
                    }
                };

                //Configuramos la seguridad
                // Esto es como el DNI de las aplicaciones, debe ser unico y oficial, pero por internet se puede conseguir uno falso, asi que ok supongo
                // Le decimos que está en un directorio ('StoreType'), en una ruta estándar de Windows,
                // y que su nombre ('SubjectName') debe coincidir con el nombre de nuestra aplicación.

                configuracion.SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = @"Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault",
                        SubjectName = Server._ApplicationName
                    },
                    // Definimos los almacenes de certificados para establecer la confianza.
                    // 'TrustedIssuerCertificates': Certificados de Autoridades de Certificación (CAs) en las que confiamos.
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
                    // 'TrustedPeerCertificates': Certificados de otras aplicaciones cliente/servidor en las que confiamos directamente.
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
                    // 'RejectedCertificateStore': Dónde mover los certificados de clientes que han intentado conectar pero han sido rechazados.
                    RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
                   
                    // Esta opción, del XML, permite aceptar automáticamente cualquier certificado de los clientes.
                    // Si está en true, el servidor aceptará certificados de clientes no confiables sin preguntar.
                    // No se deberia dejar en true en produccion, pero que protejan el servidor con un firewall
                    // y que se conecten solo los clientes de confianza,
                    AutoAcceptUntrustedCertificates = Server._AutoAcceptUntrustedCertificates
                };

                configuracion.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };
                configuracion.ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 };

                //Llamamos a validate para asegurarnos de que la configuracion es correcta, si no lo es, nos dira el error y no arrancara el servidor,
                //Mejor que salte el error aqui, que cuando arranca el server con la configuracion erronea, es mas facil depurar el fallo
                await configuracion.Validate(ApplicationType.Server);

                //Generamos la instancia de la aplicacion, aun no la ejecutamos
                _aplicacion = new ApplicationInstance(configuracion);

                

                // Aunque nos la sude los certificados de los clientes, porque para algo existe el firewall
                // Si que "necesitamos", entre muchas comillas, el certificado del servidor para que los clientes puedan conectarse a el
                // Porque quizas los clientes digan eS mAs fAcIl qUe mE cOnEcTe a El sErVeR cOn cErTiFiCaDo qUe SiN eL, y entonces no se podra conectar
                //Generamos el certificado del servidor si no existe, o lo cargamos si ya existe
                // El 'true' como primer argumento significa "crear si no existe". El '2048' es el tamaño de la clave.
                bool certificateOK = await _aplicacion.CheckApplicationInstanceCertificate(true, 2048);
                if (!certificateOK)
                {
                    // Si por alguna razón no se puede crear o encontrar el certificado
                    // lanzamos una excepción porque el servidor no puede arrancar de forma segura.
                    // Si falla aqui, seguro que es porque no tiene permisos para escribir en el directorio de certificados
                    //Facil solucion pero no la mejor, ejecuta el programa como administrador
                    //Correcta solucion, dale permisos de escritura en la carpeta al usuario que ejecuta el programa
                    throw new Exception("Error al validar el certificado de la instancia de aplicación.");
                }

                // Ahora que tenemos todo configurado, iniciamos el servidor OPC UA
                // 'StandardServer' es un metodo de la libreria
                //Nos maneja todo, nodos, conexiones, seguridad, etc.
                await _aplicacion.Start(new StandardServer());
                 Debug.WriteLine($"Servidor '{_aplicacion.ApplicationConfiguration.ApplicationName}' iniciado en '{_aplicacion.ApplicationConfiguration.ServerConfiguration.BaseAddresses[0]}'");

                //Evento de que el server esta corriendo
                Arrancar_Servidor?.Invoke(this, EventArgs.Empty);

                await Task.Delay(Timeout.Infinite, token_cancelacion);
            }
            catch (TaskCanceledException)
            {
                // Esta excepción es normal y esperada cuando se detiene el servidor.
                // Simplemente informamos de que se ha detenido correctamente.
                 Debug.WriteLine("El servidor OPC UA se ha detenido correctamente.");
                //Asignamos ese token de cancelacion a la aplicacion para poder pararla
                token_cancelacion.Register(() => _aplicacion.Stop());
            }
            catch (Exception ex)
            {
                
                //evento de que hay un error
                Error_Servidor?.Invoke(this, ex);

                
                 Debug.WriteLine("\n>>>>> SE PRODUJO UNA EXCEPCIÓN AL INICIAR EL SERVIDOR <<<<<");
                 Debug.WriteLine($"\nTipo de Excepción: {ex.GetType().FullName}");
                 Debug.WriteLine($"\n**Mensaje Principal: {ex.Message}**");

                // Si es una excepción de OPC UA, tendrá un StatusCode que es muy útil.
                if (ex is ServiceResultException sre)
                {
                     Debug.WriteLine($"\n**OPC UA StatusCode: {sre.StatusCode}**");
                }

                
                if (ex.InnerException != null)
                {
                     Debug.WriteLine("\n--- Excepción Interna (la causa más probable) ---");
                     Debug.WriteLine($"Tipo Interno: {ex.InnerException.GetType().FullName}");
                     Debug.WriteLine($"**Mensaje Interno: {ex.InnerException.Message}**");
                }

                // Muestra un MessageBox con el error para que sea visible.
                string errorMsg = $"Error al iniciar:\n{ex.Message}\n\n{(ex.InnerException != null ? ex.InnerException.Message : "")}";
                MessageBox.Show(errorMsg, "Error de Servidor OPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {

                // Este bloque se ejecuta siempre que el servidor se para, sea por stop o porque pete
                Parar_Servidor?.Invoke(this, EventArgs.Empty);
                // Limpiamos los recursos.
                _generador_Token?.Dispose();
                _generador_Token = null;
                _aplicacion = null;
            }
        }

        public void Stop()
        {
            if (_generador_Token != null && !_generador_Token.IsCancellationRequested)
            {
                 Debug.WriteLine("Recibida solicitud para detener el servidor...");
                _generador_Token.Cancel();
            }
        }


        private static void Configurar_parametros_servidor()
        {
            //Se le el XML de configuracion del servidor y se almacena la informacion en variables estaticas de la clase  
            try
            {
                //Leer el archivo XML de configuración
                if (File.Exists(LeerXML.get_ruta_xml_configuracion_server()))
                {
                    XDocument doc = XDocument.Load(LeerXML.get_ruta_xml_configuracion_server());
                     Debug.WriteLine("Archivo de configuración leído correctamente.");
                    //nodo raíz <OpcUaConfig>
                    XElement configRoot = doc.Root;

                    // Asignamos los valores a las variables estaticas

                    _ApplicationName = configRoot?.Element("ApplicationName")?.Value;
                    _ApplicationUri = configRoot?.Element("ApplicationUri")?.Value;
                    _ProductUri = configRoot?.Element("ProductUri")?.Value;
                    _ServerUrl = configRoot?.Element("ServerUrl")?.Value;

                    // Seguridad
                    XElement securityElement = configRoot?.Element("Security");
                    bool.TryParse(securityElement?.Element("AutoAcceptUntrustedCertificates")?.Value, out bool autoAccept);
                    _AutoAcceptUntrustedCertificates = autoAccept;
                     Debug.WriteLine("Lectura correcta del archivo XML");

                }
                else
                {
                     Debug.WriteLine($"Error: El archivo de configuración '{LeerXML.get_ruta_xml_configuracion_server()}' no existe.");
                }
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"Error fatal al leer o analizar el archivo de configuración: {ex.Message}");
                throw;
            }
        }
    }
}
