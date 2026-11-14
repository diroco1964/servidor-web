using System.Net.Sockets;
using System.Text;
using System.Web;

namespace ServidorWeb;

public sealed class HttpException : Exception
{
    public int StatusCode { get; }

    public HttpException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class HttpRequest
{
    public string Method { get; }
    public string Path { get; }
    public string Query { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }
    public string Body { get; }

    public HttpRequest(string method, string path, string query,
        IReadOnlyDictionary<string, string> headers, string body)
    {
        Method = method;
        Path = path;
        Query = query;
        Headers = headers;
        Body = body;
    }

    public bool AcceptsGzip =>
        Headers.TryGetValue("Accept-Encoding", out string? value)
        && value.Contains("gzip", StringComparison.OrdinalIgnoreCase);
}

public static class HttpRequestParser
{
    private const int MaxHeaderBytes = 16 * 1024;
    private const int MaxBodyBytes = 1024 * 1024;
    private static readonly byte[] HeaderEnd = { 13, 10, 13, 10 };

    public static async Task<HttpRequest?> ParseAsync(NetworkStream stream, CancellationToken token)
    {
        byte[] buffer = new byte[4096];
        using MemoryStream received = new MemoryStream();
        int headerEndIndex = -1;

        while (headerEndIndex < 0)
        {
            int read = await stream.ReadAsync(buffer, 0, buffer.Length, token);
            if (read == 0)
            {
                if (received.Length == 0)
                {
                    return null;
                }
                throw new HttpException(400, "Solicitud incompleta.");
            }

            received.Write(buffer, 0, read);
            if (received.Length > MaxHeaderBytes)
            {
                throw new HttpException(431, "Los encabezados son demasiado grandes.");
            }

            headerEndIndex = IndexOfHeaderEnd(received.GetBuffer(), (int)received.Length);
        }

        byte[] data = received.GetBuffer();
        string headerText = Encoding.UTF8.GetString(data, 0, headerEndIndex);
        string[] lines = headerText.Split("\r\n");

        string[] requestLine = lines[0].Split(' ');
        if (requestLine.Length != 3)
        {
            throw new HttpException(400, "Línea de solicitud inválida.");
        }

        string method = requestLine[0].ToUpperInvariant();
        string target = requestLine[1];
        string version = requestLine[2];

        if (!version.StartsWith("HTTP/1.", StringComparison.Ordinal))
        {
            throw new HttpException(505, "Versión de HTTP no soportada.");
        }

        if (!target.StartsWith('/'))
        {
            throw new HttpException(400, "La ruta debe comenzar con '/'.");
        }

        Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            int colon = lines[i].IndexOf(':');
            if (colon > 0)
            {
                headers[lines[i][..colon].Trim()] = lines[i][(colon + 1)..].Trim();
            }
        }

        if (headers.ContainsKey("Transfer-Encoding"))
        {
            throw new HttpException(501, "Transfer-Encoding no está soportado.");
        }

        string body = await ReadBodyAsync(stream, headers, data, (int)received.Length, headerEndIndex + 4, token);

        int questionMark = target.IndexOf('?');
        string rawPath = questionMark >= 0 ? target[..questionMark] : target;
        string rawQuery = questionMark >= 0 ? target[(questionMark + 1)..] : string.Empty;

        string path = Uri.UnescapeDataString(rawPath);
        if (path.Contains('\0'))
        {
            throw new HttpException(400, "La ruta contiene caracteres inválidos.");
        }

        return new HttpRequest(method, path, FormatQuery(rawQuery), headers, body);
    }

    private static async Task<string> ReadBodyAsync(NetworkStream stream, Dictionary<string, string> headers,
        byte[] data, int receivedLength, int bodyStart, CancellationToken token)
    {
        int contentLength = 0;
        if (headers.TryGetValue("Content-Length", out string? value))
        {
            if (!int.TryParse(value, out contentLength) || contentLength < 0)
            {
                throw new HttpException(400, "Content-Length inválido.");
            }
        }

        if (contentLength == 0)
        {
            return string.Empty;
        }

        if (contentLength > MaxBodyBytes)
        {
            throw new HttpException(413, "El cuerpo de la solicitud es demasiado grande.");
        }

        byte[] body = new byte[contentLength];
        int alreadyRead = Math.Min(receivedLength - bodyStart, contentLength);
        Array.Copy(data, bodyStart, body, 0, alreadyRead);

        int total = alreadyRead;
        while (total < contentLength)
        {
            int read = await stream.ReadAsync(body, total, contentLength - total, token);
            if (read == 0)
            {
                throw new HttpException(400, "El cuerpo de la solicitud llegó incompleto.");
            }
            total += read;
        }

        return Encoding.UTF8.GetString(body);
    }

    private static int IndexOfHeaderEnd(byte[] data, int length)
    {
        return new ReadOnlySpan<byte>(data, 0, length).IndexOf(HeaderEnd);
    }

    private static string FormatQuery(string rawQuery)
    {
        if (rawQuery.Length == 0)
        {
            return string.Empty;
        }

        System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(rawQuery);
        List<string> pairs = new List<string>();
        foreach (string? key in parameters.AllKeys)
        {
            pairs.Add($"{key ?? string.Empty}={parameters[key]}");
        }
        return string.Join(", ", pairs);
    }
}
