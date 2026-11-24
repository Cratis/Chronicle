// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Sinks;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Holds at most one pending final response while database authority is stale.
/// </summary>
/// <typeparam name="T">The serialized response type.</typeparam>
/// <param name="lease">The physical database authority.</param>
/// <param name="observer">The final response observer.</param>
internal sealed class ReadModelLeaseObservation<T>(SinkProvenanceLease lease, IObserver<T> observer) : IObserver<T>, IDisposable
{
    readonly Lock _gate = new();
    readonly Lock _delivery = new();
    readonly SingleAssignmentDisposable _source = new();
    readonly CancellationTokenSource _stop = new();
    CancellationTokenRegistration _revocation;
    T? _latest;
    bool _pending;
    long _generation;
    bool _working;
    bool _completed;
    bool _stopped;

    /// <inheritdoc/>
    public void OnNext(T value)
    {
        lock (_gate)
        {
            if (_stopped) return;
            _latest = value;
            _generation++;
            _pending = true;
            if (_working) return;
            _working = true;
        }

        _ = Pump();
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
        lock (_gate)
        {
            if (_stopped) return;
            _completed = true;
            if (_working) return;
        }

        Finish(null);
    }

    /// <inheritdoc/>
    public void OnError(Exception error) => Finish(error);

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_stopped) return;
            _stopped = true;
            _latest = default;
            _pending = false;
        }

        _stop.Cancel();
        _source.Dispose();
        _revocation.Dispose();
        _stop.Dispose();
    }

    /// <summary>
    /// Subscribes to responses and observes revocation even when the source is quiet.
    /// </summary>
    /// <param name="source">The fully processed response source.</param>
    internal void Start(IObservable<T> source)
    {
        _revocation = lease.Revoked.Register(() => OnError(Unavailable()));
        lock (_gate)
        {
            if (_stopped) return;
        }

        _source.Disposable = source.Subscribe(this);
    }

    static RpcException Unavailable() => new(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));

    async Task Pump()
    {
        try
        {
            while (await DeliverPending()) { }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception)
        {
            Finish(Unavailable());
        }
    }

    async Task<bool> DeliverPending()
    {
        await lease.WaitUntilReadable(_stop.Token);
        bool complete;
        lock (_delivery)
        {
            lock (_gate)
            {
                if (_stopped) return false;
                if (_pending && !DeliverLatest()) return true;
                complete = _completed && !_pending;
                if (!_pending && !complete)
                {
                    _working = false;
                    return false;
                }
            }
        }

        if (complete) Finish(null);
        return !complete;
    }

    bool DeliverLatest()
    {
        var generation = _generation;
        try { lease.Deliver(() => observer.OnNext(_latest!)); }
        catch (InvalidOperationException exception) when (exception.Message == "Read-model storage is not available." && !lease.Revoked.IsCancellationRequested) { return false; }
        if (_generation == generation)
        {
            _pending = false;
            _latest = default;
        }

        return true;
    }

    void Finish(Exception? error)
    {
        lock (_delivery)
        {
            lock (_gate)
            {
                if (_stopped) return;
            }

            Dispose();
            if (error is null) observer.OnCompleted();
            else observer.OnError(error);
        }
    }
}
