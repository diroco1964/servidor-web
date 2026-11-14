namespace ServidorWeb;

public sealed class RequestLogger
{
    private const int MaxLoggedBodyLength = 2000;

    private readonly object _fileLock = new object();
    private readonly string _logFolder;

    public RequestLogger(string logFolder)
    {
        _logFolder = logFolder;
        Directory.CreateDirectory(_logFolder);
    }

    public void Log(string clientIp, HttpRequest? request, HttpResponse response)
    {
        DateTime now = DateTime.Now;

        string method = request?.Method ?? "-";
        string path = request is null ? "-" : Sanitize(request.Path);
        string query = string.IsNullOrEmpty(request?.Query) ? "-" : Sanitize(request!.Query);
        string compression = response.IsGzip ? "gzip" : "none";

        string line = $"{now:yyyy-MM-dd HH:mm:ss} | {clientIp} | {method} | {path} | query: {query} | " +
                      $"status: {response.StatusCode} | bytes: {response.Body.Length} | encoding: {compression}";

        if (request is not null && method == "POST")
        {
            line += $" | body: {SanitizeBody(request.Body)}";
        }

        string filePath = Path.Combine(_logFolder, $"{now:yyyy-MM-dd}.log");

        lock (_fileLock)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }

    private static string SanitizeBody(string body)
    {
        string text = Sanitize(body);
        return text.Length > MaxLoggedBodyLength ? text[..MaxLoggedBodyLength] + "...(truncado)" : text;
    }

    private static string Sanitize(string value)
    {
        return value.Replace("\r", "\\r").Replace("\n", "\\n");
    }
}
