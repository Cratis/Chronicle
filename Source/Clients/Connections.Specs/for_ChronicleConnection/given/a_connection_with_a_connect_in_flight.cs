// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// A connection to one server whose resolution, handshake and connected handlers the spec holds and releases
/// separately, so it decides exactly which phase of the one attempt is in flight when callers arrive.
/// </summary>
public class a_connection_with_a_connect_in_flight : a_connection_with_several_endpoints
{
    protected int _resolves;
    protected int _connectedCalls;
    protected TaskCompletionSource _resolveGate;
    protected TaskCompletionSource _handshake;
    protected TaskCompletionSource _connectedGate;
    protected TaskCompletionSource _connectedEntered;
    protected Exception _connectedFailure;

    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];

    void Establish()
    {
        _resolveGate = NewGate();
        _resolveGate.SetResult();
        _handshake = NewGate();
        _connectedGate = NewGate();
        _connectedEntered = NewGate();
        _streams[_a] = () => Observable.FromAsync(() => _handshake.Task)
            .Select(_ => new ConnectionKeepAlive { ConnectionId = "connection" })
            .Concat(Observable.Never<ConnectionKeepAlive>());
    }

    protected static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override async Task<IReadOnlyList<ChronicleServerAddress>> Resolve()
    {
        Interlocked.Increment(ref _resolves);
        SignalChange();
        await _resolveGate.Task;
        return Servers;
    }

    protected override async Task OnLifecycleConnected()
    {
        // The real lifecycle sets IsConnected before the handlers run and rolls it back when one fails.
        _isConnected = true;
        Interlocked.Increment(ref _connectedCalls);
        _connectedEntered.TrySetResult();
        SignalChange();
        await _connectedGate.Task;
        if (_connectedFailure is not null)
        {
            _isConnected = false;
            throw _connectedFailure;
        }
    }

    /// <summary>
    /// Asks for the services the way an asynchronous caller does. The call runs synchronously up to its first wait, so
    /// once this returns the caller has already joined the attempt.
    /// </summary>
    /// <param name="cancellationToken">The caller's own token.</param>
    /// <returns>The pending call.</returns>
    protected Task<IServices> StartCaller(CancellationToken cancellationToken = default) =>
        _connection.GetServices(cancellationToken).AsTask();

    /// <summary>
    /// Asks for the services the way a synchronous caller does, blocking a pool thread.
    /// </summary>
    /// <returns>The pending call.</returns>
    protected Task<IServices> StartSyncCaller() =>
        Task.Run(() => ((IChronicleServicesAccessor)_connection).Services);

    /// <summary>
    /// Waits until the attempt has begun resolving servers.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected Task AttemptHasStarted() => WaitUntil(() => Volatile.Read(ref _resolves) > 0);

    /// <summary>
    /// Waits until the attempt has opened its keep-alive stream, so the handshake can be completed.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected Task HandshakeIsPending() => WaitUntil(() => _connectsPerServer.GetValueOrDefault(_a) > 0);
}
