using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualBasic.ApplicationServices;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System.Drawing;


namespace OPCplug.Code
{
    internal class Server
    {
        static string ruta_xml = Main.get_ruta_xml();
        static string ruta_log = Main.get_ruta_log();
        static string? ApplicationName;
        static string? ApplicationUri;
        static string? ProductUri;
        static string? ServerUrl;
        static bool AutoAcceptUntrustedCertificates;
        public string Id { get; } = Guid.NewGuid().ToString();

        public async Task async_server()
        {
            try
            {
                
                //LLamo a mi metodo para cargar en las variables staticas todos los datos que necesitamos para configurar el servidor
                configurar_parametros_servidor();
               
            
                //Generamos el archivo de configuracion vacio
                var configuracion = new Opc.Ua.ApplicationConfiguration();


                //Identificadores de la aplicacion, algo asi como el numero de serie o mac
                configuracion.ApplicationUri = Server.ApplicationUri;
                configuracion.ProductUri = Server.ProductUri;
                configuracion.ApplicationName = Server.ApplicationName;
                configuracion.ApplicationType = ApplicationType.Server;


                //Configuramos la conexion IP
                // Configuramos la politica de encriptamiento que "puede", repito "PUEDE" usar el server: nada, Firmado y Firmado y encriptado
                configuracion.ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = { Server.ServerUrl },
                    SecurityPolicies =
                    {
                    new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None },
                    new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.Sign, SecurityPolicyUri = SecurityPolicies.Basic256Sha256 },
                    new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.SignAndEncrypt, SecurityPolicyUri = SecurityPolicies.Basic256Sha256 }
                    }
                };

                //Configuramos la seguridad
                // Esto es como el DNI de las aplicaicones, debe ser unico y oficial, pero por internet se puede conseguir uno falso, asi que ok supongo
                // Le decimos que está en un directorio ('StoreType'), en una ruta estándar de Windows,
                // y que su nombre ('SubjectName') debe coincidir con el nombre de nuestra aplicación.

                configuracion.SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = @"Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault",
                        SubjectName = Server.ApplicationName
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
                    // No se deberia dejar en true en produccion, pero que le ***, que protejan el servidor con un firewall
                    // y que se conecten solo los clientes de confianza,
                    AutoAcceptUntrustedCertificates = Server.AutoAcceptUntrustedCertificates
                };

                configuracion.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };
                configuracion.ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 };

                //Llamamos a validate para asegurarnos de que la configuracion es correcta, si no lo es, nos dira el error y no arrancara el servidor,
                //Mejor que salte el error aqui, que cuando arranca el server con la configuracion erronea, es mas facil depurar el fallo
                await configuracion.Validate(ApplicationType.Server);

                //Generamos la instancia de la aplicacion, aun no la ejecutamos
                var aplicacion = new ApplicationInstance(configuracion);

                // Aunque nos la sude los certificados de los clientes, porque para algo existe el firewall
                // Si que "necesitamos", entre muchas comillas, el certificado del servidor para que los clientes puedan conectarse a el
                // Porque quizas los clientes digan eS mAs fAcIl qUe mE cOnEcTe a El sErVeR cOn cErTiFiCaDo qUe SiN eL, y entonces no se podra conectar
                //Generamos el certificado del servidor si no existe, o lo cargamos si ya existe
                // El 'true' como primer argumento significa "crear si no existe". El '2048' es el tamaño de la clave.
                bool certificateOK = await aplicacion.CheckApplicationInstanceCertificate(true, 2048);
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
                await aplicacion.Start(new StandardServer());
                
                Console.WriteLine($"Servidor '{aplicacion.ApplicationConfiguration.ApplicationName}' iniciado en '{aplicacion.ApplicationConfiguration.ServerConfiguration.BaseAddresses[0]}'");
            
            }
            catch (Exception ex)
            {
                // --- BLOQUE DE DEBUG MEJORADO ---
                // Este bloque nos dará la información exacta que necesitamos.
                Console.WriteLine("\n>>>>> SE PRODUJO UNA EXCEPCIÓN AL INICIAR EL SERVIDOR <<<<<");
                Console.WriteLine($"\nTipo de Excepción: {ex.GetType().FullName}");
                Console.WriteLine($"\n**Mensaje Principal: {ex.Message}**");

                // Si es una excepción de OPC UA, tendrá un StatusCode que es muy útil.
                if (ex is ServiceResultException sre)
                {
                    Console.WriteLine($"\n**OPC UA StatusCode: {sre.StatusCode}**");
                }

                // A menudo, el error real está en la "excepción interna".
                if (ex.InnerException != null)
                {
                    Console.WriteLine("\n--- Excepción Interna (la causa más probable) ---");
                    Console.WriteLine($"Tipo Interno: {ex.InnerException.GetType().FullName}");
                    Console.WriteLine($"**Mensaje Interno: {ex.InnerException.Message}**");
                }

                // Muestra un MessageBox con el error para que sea visible.
                string errorMsg = $"Error al iniciar:\n{ex.Message}\n\n{(ex.InnerException != null ? ex.InnerException.Message : "")}";
                MessageBox.Show(errorMsg, "Error de Servidor OPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void configurar_parametros_servidor()
        {
            //Se le el XML de configuracion del servidor y se almacena la informacion en variables estaticas de la clase  
            try
            {
                //Leer el archivo XML de configuración
                if (File.Exists(ruta_xml))
                {
                    XDocument doc = XDocument.Load(ruta_xml);
                    Console.WriteLine("Archivo de configuración leído correctamente.");
                    //nodo raíz <OpcUaConfig>
                    XElement configRoot = doc.Root;

                    // Asignamos los valores a las variables estaticas

                    ApplicationName = configRoot?.Element("ApplicationName")?.Value;
                    ApplicationUri = configRoot?.Element("ApplicationUri")?.Value;
                    ProductUri = configRoot?.Element("ProductUri")?.Value;
                    ServerUrl = configRoot?.Element("ServerUrl")?.Value;

                    // Seguridad
                    XElement securityElement = configRoot?.Element("Security");
                    bool.TryParse(securityElement?.Element("AutoAcceptUntrustedCertificates")?.Value, out bool autoAccept);
                    AutoAcceptUntrustedCertificates = autoAccept;
                    Console.WriteLine("Lectura correcta del archivo XML");

                }
                else
                {
                    Console.WriteLine($"Error: El archivo de configuración '{ruta_xml}' no existe.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fatal al leer o analizar el archivo de configuración: {ex.Message}");
                throw;
            }
        }
    }
}
