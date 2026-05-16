using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace exui_wpf;

public static class ExuiClient
{
    private static ClientWebSocket? _socket;
    public static TelemetrySource Telemetry { get; } = new();

    public static void Start()
    {
        _ = Task.Run(ConnectAndListen);
    }

    private static async Task ConnectAndListen()
    {
        _socket = new ClientWebSocket();
        Uri uri = new Uri("ws://localhost:8080/exui");

        try
        {
            await _socket.ConnectAsync(uri, CancellationToken.None);
            Telemetry["connected"] = true;
            byte[] buffer = new byte[2048];

            while (_socket.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;

                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                ParsePayload(json);
            }
        }
        catch
        {
            Telemetry["connected"] = false;
        }
    }

    private static void ParsePayload(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    Telemetry[property.Name] = property.Value.GetDouble();
                }
                else if (property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False)
                {
                    Telemetry[property.Name] = property.Value.GetBoolean();
                }
                else
                {
                    Telemetry[property.Name] = property.Value.ToString();
                }
            }
        }
        catch
        {
        }
    }
}