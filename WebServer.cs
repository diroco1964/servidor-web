using System.Net;
using System.Net.Sockets;

namespace ServidorWeb;

public sealed class WebServer
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly int _port;
    private readonly RequestHandler _handler;
    private readonly RequestLogger _logger;

    public WebServer(ServerConfig config, string baseDirectory)
    {
        string rootFolder = Path.GetFullPath(config.RootFolder, baseDirectory);
        string logFolder = Path.GetFullPath(config.LogFolder, baseDirectory);

        if (!Directory.Exists(rootFolder))
        {
            throw new DirectoryNotFoundException($"La carpeta a servir no existe: {rootFolder}");
        }

        _port = config.Port;
        _handler = new RequestHandler(rootFolder, config.CompressionMinBytes);
        _logger = new RequestLogger(logFolder);

        Console.WriteLine($"Carpeta a servir: {rootFolder}");
        Console.WriteLine($"Carpeta de logs:  {logFolder}");
    }

    public async Task RunAsync(CancellationToken token)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"Servidor escuchando en http://localhost:{_port}  (Ctrl+C para detener)");

        try
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(token);
                _ = HandleClientAsync(client, token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Cierre solicitado, deteniendo el servidor.");
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken serverToken)
    {
        string clientIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "desconocida";

        using (client)
        using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(serverToken))
        {
            timeout.CancelAfter(RequestTimeout);

            try
            {
                NetworkStream stream = client.GetStream();
                HttpRequest? request = null;
                HttpResponse response;

                try
                {
                    request = await HttpRequestParser.ParseAsync(stream, timeout.Token);
                    if (request is null)
                    {
                        return;
                    }
                    response = _handler.Handle(request);
                }
                catch (HttpException ex)
                {
                    response = HttpResponse.FromText(ex.StatusCode, ex.Message);
                }
                catch (Exception ex) when (ex is not (OperationCanceledException or IOException))
                {
                    Console.WriteLine($"[{clientIp}] Error procesando la solicitud: {ex}");
                    response = HttpResponse.FromText(500, "Error interno del servidor.");
                }

                await response.WriteToAsync(stream, timeout.Token);
                _logger.Log(clientIp, request, response);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[{clientIp}] Conexión cancelada por timeout o por cierre del servidor.");
            }
            catch (Exception ex) when (ex is IOException or SocketException)
            {
                Console.WriteLine($"[{clientIp}] Conexión interrumpida: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{clientIp}] Error inesperado: {ex}");
            }
        }
    }
}
