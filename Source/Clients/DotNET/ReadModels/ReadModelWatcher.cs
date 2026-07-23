// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
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
    TaskCompletionSource _subscribedTcs;
    Action? _stopped;
    IObservable<ContractReadModels.ReadModelChangeset>? _serverObservable;
    IDisposable? _serverSubscription;
    bool _started;

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
        _serverSubscription?.Dispose();
        _serverSubscription = null;
        _stopped?.Invoke();
        _stopped = null;
        _observable.Dispose();
    }

    /// <inheritdoc/>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        if (_subscribedTcs.Task.IsCompleted)
        {
            _subscribedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        // Dispose any prior subscription before opening a new server stream so a reconnect
        // never leaks the subscription established for the previous connection.
        _serverSubscription?.Dispose();

        var request = new ContractReadModels.WatchRequest
        {
            EventStore = _eventStore.Name,
            Namespace = _eventStore.Namespace,
            ReadModelIdentifier = typeof(TReadModel).GetReadModelIdentifier(),
            EventSequenceId = EventSequences.EventSequenceId.Log
        };
        _serverObservable = _servicesAccessor.Services.ReadModels.Watch(request);
        _serverSubscription = _serverObservable.Subscribe(
            changeset =>
            {
                if (changeset.Subscribed)
                {
                    _subscribedTcs.TrySetResult();
                    return;
                }

                var readModel = JsonSerializer.Deserialize<TReadModel>(changeset.ReadModel, _jsonSerializerOptions);
                _observable.OnNext(new ReadModelChangeset<TReadModel>(
                    changeset.Namespace,
                    changeset.ModelKey,
                    readModel,
                    changeset.Removed));
            },
            _ => ResetForReconnect(),
            ResetForReconnect);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _stopped?.Invoke();
        _stopped = null;
        Dispose();
    }

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
        _serverSubscription?.Dispose();
        _serverSubscription = null;
        _started = false;
    }
}
