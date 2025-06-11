using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPCplug.Code
{
    internal class LeerXML
    {   
            //RUTA CARPETA MIS DOCUMENTOS
        private static readonly string _ruta_mis_documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        //RUTA PARA EL ARCHIVO DE LOGS
        private static readonly string _ruta_carpeta_logs = System.IO.Path.Combine(_ruta_mis_documentos, "OPCplug", "Logs");
        private static readonly string _ruta_archivo_log = System.IO.Path.Combine(_ruta_carpeta_logs, "log.txt");
        //RUTA DE XML DE CONFIGURACION
        private static readonly string _ruta_carpeta_configuracion = System.IO.Path.Combine(_ruta_mis_documentos, "OPCplug", "Configuracion");
        private static readonly string _ruta_xml_configuracion_server = System.IO.Path.Combine(_ruta_carpeta_configuracion, "configuracion_server.xml");
        private static readonly string _ruta_xml_datos_cliente = System.IO.Path.Combine(_ruta_carpeta_configuracion, "datos_clientes.xml");
        private static readonly string _ruta_xml_datos_server = System.IO.Path.Combine(_ruta_carpeta_configuracion, "datos_server.xml");

        public static string get_ruta_xml_configuracion_server()
        {
            return _ruta_xml_configuracion_server;
        }
        public static string get_ruta_log()
        {
            return _ruta_archivo_log;
        }

        public static string get_ruta_xml_datos_clientes()
        {
            return _ruta_xml_datos_cliente;
        }

        public static string get_ruta_xml_datos_server()
        {
            return _ruta_xml_datos_server;
        }


        public static void InicializarEstructura()
        {
            try
            {
                // Crear carpeta Logs si no existe
                if (!Directory.Exists(_ruta_carpeta_logs))
                {
                    Directory.CreateDirectory(_ruta_carpeta_logs);
                }

                // Crear archivo de log si no existe
                if (!File.Exists(_ruta_archivo_log))
                {
                    File.WriteAllText(_ruta_archivo_log, $"[LOG INICIALIZADO] {DateTime.Now}\n");
                }

                // Crear carpeta Configuracion si no existe
                if (!Directory.Exists(_ruta_carpeta_configuracion))
                {
                    Directory.CreateDirectory(_ruta_carpeta_configuracion);
                }

                // Crear archivo de configuración XML si no existe
                if (!File.Exists(_ruta_xml_configuracion_server))
                {
                    string contenidoXml = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
                                            <!-- Archivo de configuración para el Servidor OPC UA -->
                                            <OpcUaConfig>
  
                                              <!-- Identificación de la aplicación. Esencial para que los clientes reconozcan el servidor. -->
                                              <ApplicationName>MiServidorOPC</ApplicationName>
                                              <ApplicationUri>urn:{Environment.MachineName}:SaikaDarazu:MiServidorOPC</ApplicationUri>
                                              <ProductUri>urn:SaikaDarazu:MiServidorOPC</ProductUri>
  
                                              <!-- Endpoint (Punto de conexión) principal del servidor. -->
                                              <ServerUrl>opc.tcp://{Environment.MachineName}:4840/MiServidorOPC</ServerUrl>
  
                                              <!-- Configuración de seguridad -->
                                              <Security>
                                                <!-- Políticas de seguridad soportadas. Opciones: None, Sign, SignAndEncrypt -->
                                                <Policy>None</Policy>
    
                                                <!-- Aceptar certificados de cliente no confiables automáticamente (solo para desarrollo). -->
                                                <AutoAcceptUntrustedCertificates>true</AutoAcceptUntrustedCertificates>
                                              </Security>
  
                                            </OpcUaConfig>";
                    File.WriteAllText(_ruta_xml_configuracion_server, contenidoXml);
                }

                System.Diagnostics.Debug.WriteLine("Estructura de archivos verificada/creada correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al inicializar la estructura de archivos: " + ex.Message);
            }
        }

    }
}
