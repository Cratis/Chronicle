// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using ContractReadModels = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelWatcher{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
public class ReadModelWatcher<TReadModel> : IReadModelWatcher<TReadModel>, IDisposable
{
    readonly Subject<ReadModelChangeset<TReadModel>> _observable;
    readonly IEventStore _eventStore;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly object _sync = new();
    TaskCompletionSource _subscribedTcs;
    Action? _stopped;
    IObservable<ContractReadModels.ReadModelChangeset>? _serverObservable;
    IDisposable? _serverSubscription;
    bool _started;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelWatcher{TReadModel}"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the watcher is for.</param>
    /// <param name="stopped">Callback for when the watcher is stopped.</param>
    /// <param name="jsonSerializerOptions">Options for JSON serialization.</param>
    public ReadModelWatcher(IEventStore eventStore, Action stopped, JsonSerializerOptions jsonSerializerOptions)
    {
        _observable = new Subject<ReadModelChangeset<TReadModel>>();
        _subscribedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _eventStore = eventStore;
        _stopped = stopped;
        _jsonSerializerOptions = jsonSerializerOptions;
        _eventStore.Connection.Lifecycle.OnConnected += ClientConnected;
        _eventStore.Connection.Lifecycle.OnDisconnected += ClientDisconnected;
    }

    /// <inheritdoc/>
    public IObservable<ReadModelChangeset<TReadModel>> Observable => _observable;

    /// <inheritdoc/>
    public Task Subscribed => _subscribedTcs.Task;

    /// <inheritdoc/>
    public void Dispose()
    {
        _eventStore.Connection.Lifecycle.OnConnected -= ClientConnected;
        _eventStore.Connection.Lifecycle.OnDisconnected -= ClientDisconnected;
        IDisposable? subscription;
        lock (_sync)
        {
            _disposed = true;
            subscription = _serverSubscription;
            _serverSubscription = null;
        }

        subscription?.Dispose();
        _stopped?.Invoke();
        _stopped = null;
        _observable.Dispose();
    }

    /// <inheritdoc/>
    public void Start()
    {
        IDisposable? prior;
        lock (_sync)
        {
            if (_disposed || _started)
            {
                return;
            }

            _started = true;
            if (_subscribedTcs.Task.IsCompleted)
            {
                _subscribedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            prior = _serverSubscription;
            _serverSubscription = null;
        }

        // Dispose any prior subscription before opening a new server stream so a reconnect
        // never leaks the subscription established for the previous connection.
        prior?.Dispose();

        var reducer = _eventStore.Reducers.HasFor<TReadModel>()
            ? _eventStore.Reducers.GetHandlerForReadModelType(typeof(TReadModel))
            : null;
        var request = new ContractReadModels.WatchRequest
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ReadModelIdentifier = typeof(TReadModel).GetReadModelIdentifier(),
            EventSequenceId = reducer?.EventSequenceId ?? EventSequences.EventSequenceId.Log
        };
        _serverObservable = _servicesAccessor.Services.ReadModels.Watch(request);
        var subscription = _serverObservable.Subscribe(
            changeset =>
            {
                if (changeset.Subscribed)
                {
                    _subscribedTcs.TrySetResult();
                    return;
                }

                var readModel = DeserializeReadModel(changeset.ReadModel, reducer is not null, changeset.Removed);
                var changeContext = EventContext.From(
                    _eventStore.Name,
                    changeset.Namespace,
                    EventType.Unknown,
                    EventSourceType.Default,
                    changeset.ModelKey,
                    EventStreamType.All,
                    EventStreamId.Default,
                    changeset.EventSequenceNumber,
                    changeset.CorrelationId,
                    changeset.Occurred);

                _observable.OnNext(new ReadModelChangeset<TReadModel>(
                    changeset.Namespace,
                    changeset.ModelKey,
                    readModel,
                    changeset.Removed,
                    ToChangeType(changeset.ChangeType),
                    changeContext));
            },
            _ => ResetForReconnect(),
            ResetForReconnect);

        // The stream is kept in a local until it is published, so a disposal that ran while it was being
        // created closes it here instead of leaking it. The services are never reached under the lock.
        lock (_sync)
        {
            if (!_disposed)
            {
                _serverSubscription = subscription;
                return;
            }
        }

        subscription.Dispose();
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _stopped?.Invoke();
        _stopped = null;
        Dispose();
    }

    static ReadModelChangeType ToChangeType(ContractReadModels.ReadModelChangeType changeType) => changeType switch
    {
        ContractReadModels.ReadModelChangeType.Added => ReadModelChangeType.Added,
        ContractReadModels.ReadModelChangeType.Removed => ReadModelChangeType.Removed,
        _ => ReadModelChangeType.Modified
    };

    TReadModel? DeserializeReadModel(string json, bool isReducer, bool removed) =>
        isReducer && removed ? default : JsonSerializer.Deserialize<TReadModel>(json, _jsonSerializerOptions);

    Task ClientConnected()
    {
        Start();
        return Task.CompletedTask;
    }

    Task ClientDisconnected()
    {
        ResetForReconnect();
        return Task.CompletedTask;
    }

    void ResetForReconnect()
    {
        // Tear down the current server stream and clear the started latch so the next
        // OnConnected re-subscribes. Reached both on disconnect and on a stream fault or
        // completion, so a dropped stream is never left silently non-emitting.
        IDisposable? subscription;
        lock (_sync)
        {
            subscription = _serverSubscription;
            _serverSubscription = null;
            _started = false;
        }

        subscription?.Dispose();
    }
}
