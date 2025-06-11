# OPCplug - Servidor Espejo OPC UA - WIP -

**OPCplug** es una aplicación de escritorio para Windows desarrollada en C#. Actúa como un puente o "espejo" OPC UA, permitiendo conectarse a múltiples servidores OPC UA remotos, leer un conjunto específico de nodos de cada uno y exponer todos esos datos consolidados a través de su propio servidor OPC UA local.

Está construida utilizando la biblioteca **UA-.NETStandard** de la OPC Foundation, y aprovecha el uso de threads y tasks para permitir una ejecución en paralelo y reducir tiempos de lectura.

Cada cliente OPC remoto se maneja mediante una tarea (Task) independiente, que mantiene una sesión activa con el servidor correspondiente y gestiona automáticamente la reconexión en caso de fallo. Por su parte, el servidor OPC UA local se ejecuta también en una tarea separada, lo que permite iniciar y detener tanto el servidor como la lectura de clientes de forma independiente.

Esta arquitectura permite centralizar información desde múltiples fuentes OPC UA en un único punto de acceso, lo cual es especialmente útil en entornos industriales con múltiples dispositivos o sistemas distribuidos. Puede ser utilizado por sistemas SCADA, historiadores u otras aplicaciones cliente que necesiten acceder de forma centralizada a datos distribuidos.

## Características Principales

- **Servidor OPC UA Integrado**  
  Expone todos los datos recopilados en un único endpoint, listo para ser consumido por cualquier cliente OPC UA.

- **Cliente OPC UA Múltiple**  
  Capaz de conectarse simultáneamente a varios servidores OPC UA remotos, cada uno con su propia configuración.

- **Configuración Dinámica por XML**  
  - El comportamiento del servidor local (nombre, URL, seguridad) se define en un archivo `server_config.xml`.  
  - Las conexiones a los clientes remotos (endpoints, credenciales, nodos a leer) se definen en un archivo `clients.xml`.

- **Lectura de Datos en Tiempo Real**  
  Utiliza el modelo de suscripción de OPC UA para recibir actualizaciones de datos de forma eficiente y en tiempo real.

- **Reconexión Automática**  
  Si la conexión con un servidor remoto se pierde (por reinicio del servidor, fallo de red, etc.), la aplicación intentará reconectarse automáticamente a intervalos regulares hasta restablecer la comunicación.

- **Interfaz de Usuario Sencilla**  
  Permite arrancar y detener el servidor y el proceso de lectura de clientes de forma independiente, a través de una interfaz gráfica simple con indicadores de estado visuales.

## Configuración

Para que la aplicación funcione, es necesario configurar dos archivos XML que deben estar en la misma carpeta que el ejecutable.

### 1. Configuración del Servidor (`server_config.xml`)

Este archivo define cómo se comportará tu servidor OPCplug:
```xml
<OpcUaConfig>
  <ApplicationName>OPCplug</ApplicationName>
  <ApplicationUri>urn:tu-pc:OPCplug:Server</ApplicationUri>
  <ProductUri>urn:OPCplug:Server</ProductUri>
  <ServerUrl>opc.tcp://tu-pc:49320</ServerUrl>
  <!-- ... etc ... -->
</OpcUaConfig>
```

### 2. Configuración de Clientes (`clients.xml`)
Aquí se define a qué servidores remotos se conectará la aplicación para leer datos:
```xml
<ClientConfigurations>
  <Client name="NombreDelCliente">
    <EndpointUrl>opc.tcp://direccion.ip.remota:4840</EndpointUrl>
    <UserIdentity>
      <UserName>usuario</UserName>
      <Password>contraseña</Password>
    </UserIdentity>
    <NodesToRead>
      <Node nodeId="ns=4;i=50" displayName="NombreParaElEspejo"/>
    </NodesToRead>
  </Client>
</ClientConfigurations>
```
### Cómo Usar
La interfaz principal contiene dos botones para controlar las funciones principales:

**Arrancar Servidor
Inicia el servidor OPC UA local de OPCplug. Una vez arrancado, este botón se convierte en "Detener Servidor".

**Arrancar Lectura Clientes
Inicia las tareas de fondo que se conectan a todos los servidores definidos en clients.xml y comienzan a monitorizar los nodos.
El botón cambia a "Detener Lectura" mientras está activo.

Ambos procesos funcionan de forma independiente y en paralelo, gracias al uso de tareas asíncronas.

