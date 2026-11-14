using System.Text.Json;

namespace ServidorWeb;

public sealed class ServerConfig
{
    public int Port { get; set; } = 8080;
    public string RootFolder { get; set; } = "www";
    public string LogFolder { get; set; } = "logs";
    public int CompressionMinBytes { get; set; } = 200;

    public static ServerConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No se encontró el archivo de configuración: {path}");
        }

        string json = File.ReadAllText(path);
        JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        ServerConfig? config = JsonSerializer.Deserialize<ServerConfig>(json, options);

        if (config is null)
        {
            throw new InvalidDataException("El archivo de configuración está vacío o es inválido.");
        }

        if (config.Port < 1 || config.Port > 65535)
        {
            throw new InvalidDataException($"Puerto inválido en la configuración: {config.Port}");
        }

        return config;
    }
}
