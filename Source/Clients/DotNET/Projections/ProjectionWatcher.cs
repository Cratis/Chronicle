// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionWatcher{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
public class ProjectionWatcher<TReadModel> : IProjectionWatcher<TReadModel>, IDisposable
{
    readonly Subject<ProjectionChangeset<TReadModel>> _observable;
    readonly IEventStore _eventStore;
    readonly IChronicleServicesAccessor _servicesAccessor;
    Action? _stopped;
    IObservable<ProjectionChangeset>? _serverObservable;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionWatcher{TReadModel}"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the watcher is for.</param>
    /// <param name="stopped">Callback for when the watcher is stopped.</param>
    public ProjectionWatcher(IEventStore eventStore, Action stopped)
    {
        _observable = new Subject<ProjectionChangeset<TReadModel>>();
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _eventStore = eventStore;
        _stopped = stopped;
        _eventStore.Connection.Lifecycle.OnConnected += ClientConnected;
    }

    /// <inheritdoc/>
    public IObservable<ProjectionChangeset<TReadModel>> Observable => _observable;

    /// <inheritdoc/>
    public void Dispose()
    {
        _eventStore.Connection.Lifecycle.OnConnected += ClientConnected;
        _stopped?.Invoke();
        _stopped = null;
        _observable.Dispose();
    }

    /// <inheritdoc/>
    public void Start()
    {
        var request = new ProjectionWatchRequest
        {
            ProjectionId = _eventStore.Projections.GetProjectionIdForModel<TReadModel>(),
            EventStore = _eventStore.Name
        };
        _serverObservable = _servicesAccessor.Services.Projections.Watch(request);
        _serverObservable.Subscribe(_ => _observable.OnNext(_.ToClient<TReadModel>()));
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
}
