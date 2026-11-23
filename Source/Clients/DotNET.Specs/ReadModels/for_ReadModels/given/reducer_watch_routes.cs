// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.given;

public class reducer_watch_routes : all_dependencies
{
    protected virtual bool Active => true;
    protected virtual bool HasEvents => true;
    protected readonly Subject<ReadModelChangeset<Model>> _server = new();
    protected readonly List<ReadModelChangeset<Model>> _changes = [];
    protected readonly EventContext _context = EventContext.EmptyWithEventSourceId("key");
    protected ReducerWatcher<Model> _local;
    protected IReducerHandler _handler;
    protected IReadModelWatcher<Model> _watcher;
    protected IDisposable _subscription;

    void Establish()
    {
        _reducers.HasFor<Model>().Returns(true);
        _handler = Substitute.For<IReducerHandler>();
        _handler.IsActive.Returns(Active);
        _handler.EventTypes.Returns(HasEvents ? [EventType.Unknown] : []);
        _reducers.GetHandlerForReadModelType(typeof(Model)).Returns(_handler);
        _watcher = Substitute.For<IReadModelWatcher<Model>>();
        _watcher.Observable.Returns(_server);
        _watcher.Subscribed.Returns(Task.CompletedTask);
        _readModelWatcherManager.GetWatcher<Model>().Returns(_watcher);
        _local = new ReducerWatcher<Model>();
        _reducerObservers.GetWatcher<Model>().Returns(_local);
    }

    protected void Subscribe() => _subscription = _readModels.Watch<Model>().Subscribe(_changes.Add);

    protected void EmitBoth()
    {
        _server.OnNext(new("tenant", "key", new Model(), false, ReadModelChangeType.Added, _context));
        _server.OnNext(new("tenant", "key", new Model(), false, ReadModelChangeType.Modified, _context));
        _local.NotifyChange("tenant", "key", new Model(), false);
        _local.NotifyChange("tenant", "key", null, true);
    }

    void Destroy()
    {
        _subscription?.Dispose();
        _local.Dispose();
        _server.Dispose();
    }

    public record Model;
}
