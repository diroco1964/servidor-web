using System.Net.Sockets;
using ServidorWeb;

string baseDirectory = AppContext.BaseDirectory;

try
{
    ServerConfig config = ServerConfig.Load(Path.Combine(baseDirectory, "config.json"));
    WebServer server = new WebServer(config, baseDirectory);

    using CancellationTokenSource cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    await server.RunAsync(cts.Token);
}
catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or SocketException)
{
    Console.WriteLine($"No se pudo iniciar el servidor: {ex.Message}");
    return 1;
}

return 0;
