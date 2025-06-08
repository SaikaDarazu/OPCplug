using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPCplug.Code
{
    internal class Main
    {   
            //RUTA CARPETA MIS DOCUMENTOS
        private static readonly string ruta_mis_documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        //RUTA PARA EL ARCHIVO DE LOGS
        private static readonly string ruta_carpeta_logs = System.IO.Path.Combine(ruta_mis_documentos, "OPCplug", "Logs");
        private static readonly string ruta_archivo_log = System.IO.Path.Combine(ruta_carpeta_logs, "log.txt");
            //RUTA DE XML DE CONFIGURACION
        private static readonly string ruta_carpeta_configuracion = System.IO.Path.Combine(ruta_mis_documentos, "OPCplug", "Configuracion");
            //RUTA XML SERVIDOR
        private static readonly string ruta_xml = System.IO.Path.Combine(ruta_carpeta_configuracion, "configuracion_server.xml");

        public static string get_ruta_xml()
        {
            return ruta_xml;
        }
        public static string get_ruta_log()
        {
            return ruta_archivo_log;
        }

        public static void InicializarEstructura()
        {
            try
            {
                // Crear carpeta Logs si no existe
                if (!Directory.Exists(ruta_carpeta_logs))
                {
                    Directory.CreateDirectory(ruta_carpeta_logs);
                }

                // Crear archivo de log si no existe
                if (!File.Exists(ruta_archivo_log))
                {
                    File.WriteAllText(ruta_archivo_log, $"[LOG INICIALIZADO] {DateTime.Now}\n");
                }

                // Crear carpeta Configuracion si no existe
                if (!Directory.Exists(ruta_carpeta_configuracion))
                {
                    Directory.CreateDirectory(ruta_carpeta_configuracion);
                }

                // Crear archivo de configuración XML si no existe
                if (!File.Exists(ruta_xml))
                {
                    string contenidoXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Configuracion>\n  <Servidor>OPCplug</Servidor>\n</Configuracion>";
                    File.WriteAllText(ruta_xml, contenidoXml);
                }

                Console.WriteLine("Estructura de archivos verificada/creada correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al inicializar la estructura de archivos: " + ex.Message);
            }
        }

    }
}
