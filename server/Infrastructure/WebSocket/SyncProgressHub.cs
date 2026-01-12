using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RiotProxy.Infrastructure.WebSocket;

/// <summary>
/// Manages WebSocket connections for sync progress updates.
/// Handles client subscribe/unsubscribe and broadcasts progress to subscribed clients.
/// </summary>
public sealed class SyncProgressHub : ISyncProgressBroadcaster
{
    private readonly ILogger<SyncProgressHub> _logger;
    
    // Connected clients: ConnectionId -> ClientConnection
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();

    // Subscriptions: Puuid -> Set of ConnectionIds
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _subscriptions = new(StringComparer.OrdinalIgnoreCase);

    public SyncProgressHub(ILogger<SyncProgressHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles a new WebSocket connection. Runs for the lifetime of the connection.
    /// </summary>
    public async Task HandleConnectionAsync(System.Net.WebSockets.WebSocket webSocket, long userId, CancellationToken ct)
    {
        var connectionId = Guid.NewGuid().ToString("N");
        var connection = new ClientConnection(connectionId, webSocket, userId);
        
        _connections[connectionId] = connection;
        _logger.LogDebug("WebSocket connected: {ConnectionId} for user {UserId}", connectionId, userId);
        
        try
        {
            await ReceiveMessagesAsync(connection, ct);
        }
        finally
        {
            // Cleanup: remove from all subscriptions
            foreach (var riotAccountId in connection.SubscribedAccounts)
            {
                Unsubscribe(connectionId, riotAccountId);
            }
            _connections.TryRemove(connectionId, out _);
            _logger.LogDebug("WebSocket disconnected: {ConnectionId}", connectionId);
        }
    }

    private async Task ReceiveMessagesAsync(ClientConnection connection, CancellationToken ct)
    {
        var buffer = new byte[1024];
        
        while (!ct.IsCancellationRequested && connection.WebSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await connection.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await connection.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "Client requested close", 
                        CancellationToken.None);
                    break;
                }
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleClientMessageAsync(connection, message);
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                // Client disconnected abruptly
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private Task HandleClientMessageAsync(ClientConnection connection, string messageJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(messageJson);
            var type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "subscribe":
                    var subscribePuuid = doc.RootElement.GetProperty("puuid").GetString();
                    if (!string.IsNullOrEmpty(subscribePuuid))
                    {
                        Subscribe(connection.ConnectionId, connection.UserId, subscribePuuid);
                        connection.SubscribedAccounts.Add(subscribePuuid);
                    }
                    break;

                case "unsubscribe":
                    var unsubscribePuuid = doc.RootElement.GetProperty("puuid").GetString();
                    if (!string.IsNullOrEmpty(unsubscribePuuid))
                    {
                        Unsubscribe(connection.ConnectionId, unsubscribePuuid);
                        connection.SubscribedAccounts.Remove(unsubscribePuuid);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown WebSocket message type: {Type}", type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse WebSocket message: {Message}", messageJson);
        }

        return Task.CompletedTask;
    }

    private void Subscribe(string connectionId, long userId, string puuid)
    {
        // TODO: Optionally verify user owns this puuid via V2RiotAccountsRepository
        var subscribers = _subscriptions.GetOrAdd(puuid, _ => new ConcurrentDictionary<string, byte>());
        subscribers[connectionId] = 0;
        _logger.LogDebug("Connection {ConnectionId} subscribed to account {Puuid}", connectionId, puuid);
    }

    private void Unsubscribe(string connectionId, string puuid)
    {
        if (_subscriptions.TryGetValue(puuid, out var subscribers))
        {
            subscribers.TryRemove(connectionId, out _);
            _logger.LogDebug("Connection {ConnectionId} unsubscribed from account {Puuid}", connectionId, puuid);
        }
    }

    // ISyncProgressBroadcaster implementation
    public async Task BroadcastProgressAsync(string puuid, int progress, int total, string? currentMatchId = null)
    {
        var message = new SyncProgressMessage
        {
            Puuid = puuid,
            Status = "syncing",
            Progress = progress,
            Total = total,
            MatchId = currentMatchId
        };
        await BroadcastToSubscribersAsync(puuid, message);
    }

    public async Task BroadcastCompleteAsync(string puuid, int totalSynced)
    {
        var message = new SyncCompleteMessage
        {
            Puuid = puuid,
            Status = "completed",
            TotalSynced = totalSynced
        };
        await BroadcastToSubscribersAsync(puuid, message);
    }

    public async Task BroadcastErrorAsync(string puuid, string error)
    {
        var message = new SyncErrorMessage
        {
            Puuid = puuid,
            Status = "failed",
            Error = error
        };
        await BroadcastToSubscribersAsync(puuid, message);
    }

    private async Task BroadcastToSubscribersAsync<T>(string puuid, T message) where T : SyncServerMessage
    {
        if (!_subscriptions.TryGetValue(puuid, out var subscribers))
            return;

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var connectionId in subscribers.Keys)
        {
            if (_connections.TryGetValue(connectionId, out var connection) &&
                connection.WebSocket.State == WebSocketState.Open)
            {
                try
                {
                    await connection.SendLock.WaitAsync();
                    try
                    {
                        await connection.WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    finally
                    {
                        connection.SendLock.Release();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send WebSocket message to {ConnectionId}", connectionId);
                }
            }
        }
    }

    private sealed class ClientConnection
    {
        public string ConnectionId { get; }
        public System.Net.WebSockets.WebSocket WebSocket { get; }
        public long UserId { get; }
        public HashSet<string> SubscribedAccounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SemaphoreSlim SendLock { get; } = new(1, 1);

        public ClientConnection(string connectionId, System.Net.WebSockets.WebSocket webSocket, long userId)
        {
            ConnectionId = connectionId;
            WebSocket = webSocket;
            UserId = userId;
        }
    }
}

