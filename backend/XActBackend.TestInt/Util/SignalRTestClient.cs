using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using XActBackend.Realtime;
using XActBackend.Shared;

namespace XActBackend.TestInt.Util;

internal sealed class SignalRTestClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly Channel<RealtimeEventEnvelope> _events = Channel.CreateUnbounded<RealtimeEventEnvelope>();
    private readonly Channel<GameSessionSnapshot> _snapshots = Channel.CreateUnbounded<GameSessionSnapshot>();

    private SignalRTestClient(HubConnection connection)
    {
        _connection = connection;

        _connection.On<RealtimeEventEnvelope>(RealtimeMethods.Event, envelope => _events.Writer.TryWrite(envelope));
        _connection.On<GameSessionSnapshot>(RealtimeMethods.Snapshot, snapshot => _snapshots.Writer.TryWrite(snapshot));
    }

    public static async ValueTask<SignalRTestClient> ConnectAsync(WebApiTestFixture fixture, CancellationToken cancellationToken)
    {
        var hubUri = new Uri(fixture.Client.BaseAddress!, "/hubs/game-session");

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.HttpMessageHandlerFactory = _ => fixture.AppFactory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .AddJsonProtocol(options => JsonConfig.ConfigureJsonSerialization(options.PayloadSerializerOptions, isDev: false))
            .Build();

        var client = new SignalRTestClient(connection);
        await client._connection.StartAsync(cancellationToken);

        return client;
    }

    public async ValueTask SubscribeSessionAsync(int sessionId, CancellationToken cancellationToken)
    {
        _ = await _connection.InvokeAsync<GameSessionSnapshot>(nameof(GameSessionHub.SubscribeSession), sessionId, cancellationToken);
    }

    public ValueTask<GameSessionSnapshot> SubscribeSessionAndGetSnapshotAsync(int sessionId, CancellationToken cancellationToken) =>
        new(_connection.InvokeAsync<GameSessionSnapshot>(nameof(GameSessionHub.SubscribeSession), sessionId, cancellationToken));

    public async ValueTask RegisterMemberPresenceAsync(
        int sessionId,
        int teamId,
        int memberId,
        int? userId,
        string? guestName,
        CancellationToken cancellationToken)
    {
        await _connection.InvokeAsync(
            nameof(GameSessionHub.RegisterMemberPresence),
            sessionId,
            teamId,
            memberId,
            userId,
            guestName,
            cancellationToken);
    }

    public async ValueTask UnregisterMemberPresenceAsync(CancellationToken cancellationToken)
    {
        await _connection.InvokeAsync(nameof(GameSessionHub.UnregisterMemberPresence), cancellationToken);
    }

    public ValueTask<RealtimeEventEnvelope?> TryReadEventAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        TryReadAsync(_events.Reader, timeout, cancellationToken);

    public ValueTask<GameSessionSnapshot?> TryReadSnapshotAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        TryReadAsync(_snapshots.Reader, timeout, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private static async ValueTask<T?> TryReadAsync<T>(ChannelReader<T> reader, TimeSpan timeout, CancellationToken cancellationToken)
        where T : class
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            return await reader.ReadAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
