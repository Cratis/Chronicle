// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Maintains a materialized reactor's stream and snapshot across connection changes.
/// </summary>
/// <typeparam name="TReadModel">The type of read model to observe.</typeparam>
/// <param name="eventStore">The event store to observe.</param>
/// <param name="differ">The differ retained across subscriptions.</param>
/// <param name="dispatch">Dispatches a change detected in the materialized window.</param>
/// <param name="failed">Reports an observation failure.</param>
internal sealed class MaterializedReadModelSubscription<TReadModel>(
    IEventStore eventStore,
    MaterializedReadModelDiffer differ,
    Action<MaterializedReadModelChange> dispatch,
    Action<Exception> failed) : IDisposable
{
    readonly object _sync = new();
    readonly object _diffSync = new();
    IDisposable? _subscription;
    long _generation;
    bool _subscribing;
    bool _started;
    bool _disposed;

    /// <summary>
    /// Starts observing materialized windows and connection lifecycle changes.
    /// </summary>
    public void Start()
    {
        lock (_sync)
        {
            if (_disposed || _started) return;
            _started = true;
            eventStore.Connection.Lifecycle.OnConnected += Connected;
            eventStore.Connection.Lifecycle.OnDisconnected += Disconnected;
        }

        try
        {
            Subscribe();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            eventStore.Connection.Lifecycle.OnConnected -= Connected;
            eventStore.Connection.Lifecycle.OnDisconnected -= Disconnected;
        }
        ResetIf();
    }

    void Subscribe()
    {
        long generation;
        lock (_sync)
        {
            if (_disposed || _subscribing || _subscription is not null) return;
            generation = ++_generation;
            _subscribing = true;
        }

        IDisposable subscription;
        try
        {
            subscription = eventStore.ReadModels.Materialized.ObserveInstances<TReadModel>(take: InstanceCount.Unlimited).Subscribe(
                window =>
                {
                    lock (_diffSync)
                    {
                        if (generation != Volatile.Read(ref _generation)) return;
                        foreach (var change in differ.Diff(window.Cast<object>())) dispatch(change);
                    }
                },
                error =>
                {
                    failed(error);
                    ResetIf(generation);
                },
                () => ResetIf(generation));
        }
        catch
        {
            ResetIf(generation);
            throw;
        }

        lock (_sync)
        {
            if (!_disposed && generation == _generation)
            {
                _subscription = subscription;
                _subscribing = false;
                return;
            }
        }
        subscription.Dispose();
    }

    Task Connected()
    {
        Subscribe();
        return Task.CompletedTask;
    }

    Task Disconnected()
    {
        ResetIf();
        return Task.CompletedTask;
    }

    void ResetIf(long? generation = null)
    {
        IDisposable? subscription;
        lock (_sync)
        {
            if (generation.HasValue && generation.Value != _generation) return;
            ++_generation;
            _subscribing = false;
            subscription = _subscription;
            _subscription = null;
        }
        subscription?.Dispose();
    }
}
