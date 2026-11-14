# ServidorWeb

Servidor web simple en C# / .NET 8 (Programación sobre Redes, proyecto final).

## Características

- Conexiones concurrentes ilimitadas (async por conexión sobre `TcpListener`).
- Sirve `index.html` si la URL no nombra un archivo.
- Puerto, carpeta raíz, carpeta de logs y umbral de compresión en `config.json`.
- 404 con documento propio (`www/404.html`).
- GET y POST (el body del POST solo se loguea); los parámetros de la URL solo se loguean.
- Compresión GZip cuando el cliente envía `Accept-Encoding: gzip` (solo tipos de texto).
- Un archivo de log por día (`logs/yyyy-MM-dd.log`) con la IP de origen.

## Configuración

```json
{
  "Port": 8080,
  "RootFolder": "www",
  "LogFolder": "logs",
  "CompressionMinBytes": 200
}
```

## Ejecutar

```
dotnet run
```

Abrir http://localhost:8080. Ctrl+C detiene el servidor.

## Estructura

| Archivo | Responsabilidad |
|---|---|
| Program.cs | Arranque, config, Ctrl+C |
| ServerConfig.cs | Lectura y validación de config.json |
| WebServer.cs | Listener y manejo de cada conexión |
| HttpRequest.cs | Modelo y parser HTTP |
| HttpResponse.cs | Modelo y escritura de la respuesta |
| RequestHandler.cs | Lógica: archivos, 404, traversal, GZip |
| RequestLogger.cs | Log diario con lock |
