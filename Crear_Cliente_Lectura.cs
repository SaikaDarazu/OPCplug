using System;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace OPCplug
{
    public static class Crear_Cliente_Lectura
    {
        public static async Task<Session> Crear_Cliente_Lectura_Async(string endpoint, string? usuario = null, string? contraseña = null, bool usar_seguridad = true)
        {
            ApplicationConfiguration configuracion_cliente;

            try
            {
                configuracion_cliente = new ApplicationConfiguration
                {
                    ApplicationName = "Cliente_Lectura_OPC",
                    ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:Cliente_Lectura_OPC",
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

                await configuracion_cliente.Validate(ApplicationType.Client);
            }
            catch (Exception ex)
            {
                throw new Exception("Error en la configuración del cliente OPC UA.", ex);
            }

            try
            {
                if (usar_seguridad)
                {
                    var aplicacion = new ApplicationInstance
                    {
                        ApplicationName = "Cliente_Lectura_OPC",
                        ApplicationType = ApplicationType.Client,
                        ApplicationConfiguration = configuracion_cliente
                    };

                    bool haveAppCertificate = await aplicacion.CheckApplicationInstanceCertificate(false, 0);
                    if (!haveAppCertificate)
                    {
                        throw new Exception("No se pudo crear un certificado de aplicación.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error verificando o generando el certificado de cliente.", ex);
            }

            ConfiguredEndpoint configuredEndpoint;
            try
            {
                var endpointDescription = CoreClientUtils.SelectEndpoint(endpoint, usar_seguridad);
                var endpointConfiguration = EndpointConfiguration.Create(configuracion_cliente);
                configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
            }
            catch (Exception ex)
            {
                throw new Exception("Error seleccionando el endpoint del servidor OPC UA.", ex);
            }

            IUserIdentity identidad;
            try
            {
                identidad = (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contraseña))
                    ? new UserIdentity(new AnonymousIdentityToken())
                    : new UserIdentity(usuario, contraseña);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creando la identidad de usuario para la sesión OPC.", ex);
            }

            try
            {
                var session = await Session.Create(
                    configuracion_cliente,
                    configuredEndpoint,
                    false,
                    "Cliente_Lectura_OPC",
                    60000,
                    identidad,
                    null
                );
                return session;
            }
            catch (Exception ex)
            {
                throw new Exception("Error creando la sesión OPC UA.", ex);
            }
        }
    }
}
