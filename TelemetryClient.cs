using System;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace exui_wpf;

public class TelemetryClient
{
    private readonly Telemetry _telemetry;
    private readonly Uri _uri = new("ws://localhost:8080/exui");

    public TelemetryClient(Telemetry telemetry)
    {
        _telemetry = telemetry;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                AppLogger.Log("Attempting connection to " + _uri);
                using var client = new ClientWebSocket();
                await client.ConnectAsync(_uri, cancellationToken);
                AppLogger.Log("Socket connected successfully.");

                var buffer = new byte[4096];

                while (client.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    
                    do
                    {
                        result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        ms.Write(buffer, 0, result.Count);
                    } 
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        AppLogger.Log("Server closed the connection thread cleanly.");
                        break;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    using var doc = await JsonDocument.ParseAsync(ms, cancellationToken: cancellationToken);
                    
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        switch (prop.Value.ValueKind)
                        {
                            case JsonValueKind.Number:
                                _telemetry[prop.Name] = prop.Value.GetSingle();
                                break;
                                
                            case JsonValueKind.String:
                                _telemetry[prop.Name] = prop.Value.GetString() ?? string.Empty;
                                break;
                                
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                _telemetry[prop.Name] = prop.Value.GetBoolean();
                                break;
                                
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("Network Exception: " + ex.Message);
                await Task.Delay(2000, cancellationToken);
            }
        }
    }
}