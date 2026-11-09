// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Maintains a materialized reactor's stream and snapshot across connection changes.
/// </summary>
/// <remarks>
/// The stream is dropped when the connection drops, and is only re-established through <see cref="Resume"/>, which the
/// event store reaches after it has registered its artifacts again. Re-subscribing straight from the connected signal
/// would race that registration - observing a read model the kernel does not know yet - and a failure thrown there
/// would fail the whole connection.
/// </remarks>
/// <typeparam name="TReadModel">The type of read model to observe.</typeparam>
/// <param name="eventStore">The event store to observe.</param>
/// <param name="differ">The differ retained across subscriptions.</param>
/// <param name="dispatch">Dispatches a change detected in the materialized window.</param>
/// <param name="failed">Reports an observation failure.</param>
internal sealed class MaterializedReadModelSubscription<TReadModel>(
    IEventStore eventStore,
    MaterializedReadModelDiffer differ,
    Action<MaterializedReadModelChange> dispatch,
    Action<Exception> failed) : IResumableReadModelSubscription
{
    readonly object _sync = new();
    readonly object _diffSync = new();
    IDisposable? _subscription;
    long _generation;
    bool _subscribing;
    bool _started;
    bool _disposed;

    /// <summary>
    /// Starts observing materialized windows and connection drops.
    /// </summary>
    public void Start()
    {
        lock (_sync)
        {
            if (_disposed || _started) return;
            _started = true;
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
    public void Resume()
    {
        try
        {
            Subscribe();
        }
        catch (Exception error)
        {
            failed(error);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
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
