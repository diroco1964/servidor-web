using System.Net.Sockets;
using System.Text;

namespace ServidorWeb;

public sealed class HttpResponse
{
    public int StatusCode { get; }
    public string ReasonPhrase { get; }
    public string ContentType { get; }
    public byte[] Body { get; }
    public bool IsGzip { get; }
    public string? Allow { get; }

    public HttpResponse(int statusCode, string reasonPhrase, string contentType, byte[] body,
        bool isGzip = false, string? allow = null)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        ContentType = contentType;
        Body = body;
        IsGzip = isGzip;
        Allow = allow;
    }

    public static HttpResponse FromText(int statusCode, string message)
    {
        return new HttpResponse(statusCode, ReasonFor(statusCode), "text/plain; charset=utf-8",
            Encoding.UTF8.GetBytes(message));
    }

    public async Task WriteToAsync(NetworkStream stream, CancellationToken token)
    {
        StringBuilder head = new StringBuilder();
        head.Append($"HTTP/1.1 {StatusCode} {ReasonPhrase}\r\n");
        head.Append($"Content-Type: {ContentType}\r\n");
        head.Append($"Content-Length: {Body.Length}\r\n");
        if (IsGzip)
        {
            head.Append("Content-Encoding: gzip\r\n");
        }
        head.Append("Vary: Accept-Encoding\r\n");
        if (Allow is not null)
        {
            head.Append($"Allow: {Allow}\r\n");
        }
        head.Append("Connection: close\r\n\r\n");

        byte[] headBytes = Encoding.ASCII.GetBytes(head.ToString());
        await stream.WriteAsync(headBytes, 0, headBytes.Length, token);
        await stream.WriteAsync(Body, 0, Body.Length, token);
        await stream.FlushAsync(token);
    }

    private static string ReasonFor(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            400 => "Bad Request",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            413 => "Payload Too Large",
            431 => "Request Header Fields Too Large",
            500 => "Internal Server Error",
            501 => "Not Implemented",
            505 => "HTTP Version Not Supported",
            _ => "Error"
        };
    }
}
