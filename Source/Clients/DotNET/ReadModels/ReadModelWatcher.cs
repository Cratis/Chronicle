// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
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
    readonly Lock _sync = new();
    TaskCompletionSource _subscribedTcs;
    Action? _stopped;
    IObservable<ContractReadModels.ReadModelChangeset>? _serverObservable;
    IDisposable? _serverSubscription;
    bool _started;
    bool _isReducer;
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
            if (_isReducer)
            {
                _subscribedTcs.TrySetCanceled();
            }
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
        TaskCompletionSource subscribed;
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

            subscribed = _subscribedTcs;
            prior = _serverSubscription;
            _serverSubscription = null;
        }

        // Dispose any prior subscription before opening a new server stream so a reconnect
        // never leaks the subscription established for the previous connection.
        prior?.Dispose();

        var isReducer = _eventStore.Reducers.HasFor<TReadModel>();
        _isReducer = isReducer;
        try
        {
            SubscribeToServer(isReducer, subscribed);
        }
        catch (Exception error) when (isReducer)
        {
            StreamEnded(subscribed, error);
        }
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

    void SubscribeToServer(bool isReducer, TaskCompletionSource subscribed)
    {
        var eventSequenceId = isReducer
            ? _eventStore.Reducers.GetHandlerForReadModelType(typeof(TReadModel)).EventSequenceId
            : EventSequenceId.Log;
        var request = new ContractReadModels.WatchRequest
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ReadModelIdentifier = typeof(TReadModel).GetReadModelIdentifier(),
            EventSequenceId = eventSequenceId
        };
        _serverObservable = _servicesAccessor.Services.ReadModels.Watch(request);
        var subscription = _serverObservable.Subscribe(
            changeset => OnServerChangeset(changeset, isReducer, subscribed),
            error => StreamEnded(subscribed, error),
            () => StreamEnded(subscribed));

        // The stream is kept in a local until it is published, so a disposal that ran while it was being
        // created closes it here instead of leaking it. The services are never reached under the lock.
        lock (_sync)
        {
            if (CanPublish(subscribed))
            {
                _serverSubscription = subscription;
                return;
            }
        }

        subscription.Dispose();
    }

    void OnServerChangeset(ContractReadModels.ReadModelChangeset changeset, bool isReducer, TaskCompletionSource subscribed)
    {
        if (changeset.Subscribed)
        {
            subscribed.TrySetResult();
            return;
        }

        _observable.OnNext(ToChangeset(changeset, isReducer));
    }

    void StreamEnded(TaskCompletionSource subscribed, Exception? error = null) => ResetForReconnect(subscribed, error: error);

    bool CanPublish(TaskCompletionSource subscribed)
    {
        if (_disposed) return false;
        if (!_isReducer) return true;
        return _started && ReferenceEquals(subscribed, _subscribedTcs);
    }

    ReadModelChangeset<TReadModel> ToChangeset(ContractReadModels.ReadModelChangeset changeset, bool isReducer) => new(
        changeset.Namespace,
        changeset.ModelKey,
        isReducer && changeset.Removed ? default : JsonSerializer.Deserialize<TReadModel>(changeset.ReadModel, _jsonSerializerOptions),
        changeset.Removed,
        ToChangeType(changeset.ChangeType),
        EventContext.From(
            _eventStore.Name,
            changeset.Namespace,
            EventType.Unknown,
            EventSourceType.Default,
            changeset.ModelKey,
            EventStreamType.All,
            EventStreamId.Default,
            changeset.EventSequenceNumber,
            changeset.CorrelationId,
            changeset.Occurred));

    Task ClientConnected()
    {
        Start();
        return Task.CompletedTask;
    }

    Task ClientDisconnected()
    {
        ResetForReconnect(renewReadiness: true);
        return Task.CompletedTask;
    }

    void ResetForReconnect(TaskCompletionSource? endedSubscription = null, bool renewReadiness = false, Exception? error = null)
    {
        // Tear down the current server stream and clear the started latch so the next
        // OnConnected re-subscribes. Reached both on disconnect and on a stream fault or
        // completion, so a dropped stream is never left silently non-emitting.
        IDisposable? subscription;
        lock (_sync)
        {
            if (endedSubscription is not null && !ReferenceEquals(endedSubscription, _subscribedTcs)) return;
            subscription = _serverSubscription;
            _serverSubscription = null;
            _started = false;
            EndReadiness(endedSubscription, renewReadiness, error);
        }

        subscription?.Dispose();
    }

    void EndReadiness(TaskCompletionSource? endedSubscription, bool renewReadiness, Exception? error)
    {
        if (!_isReducer || _disposed) return;
        if (endedSubscription is not null && _subscribedTcs.Task.IsCompletedSuccessfully)
        {
            _subscribedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        if (error is not null) _subscribedTcs.TrySetException(error);
        else _subscribedTcs.TrySetCanceled();

        if (renewReadiness)
        {
            _subscribedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
