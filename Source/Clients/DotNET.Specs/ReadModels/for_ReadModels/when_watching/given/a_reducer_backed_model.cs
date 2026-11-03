// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.given;

public class a_reducer_backed_model : for_ReadModels.given.all_dependencies
{
    protected IReducerHandler _handler;
    protected readonly Subject<ReducerChangeset<Model>> _local = new();
    protected readonly Subject<ReadModelChangeset<Model>> _server = new();
    protected readonly List<ReadModelChangeset<Model>> _changes = [];
    protected IDisposable _subscription;
    protected readonly Model _model = new("value");
    protected readonly EventContext _context = EventContext.EmptyWithEventSourceId("key") with { SequenceNumber = 42 };

    void Establish()
    {
        _reducers.HasFor<Model>().Returns(true);
        _handler = Substitute.For<IReducerHandler>();
        _handler.IsActive.Returns(true);
        _handler.EventTypes.Returns([EventType.Unknown]);
        _reducers.GetHandlerForReadModelType(typeof(Model)).Returns(_handler);
        _reducerObservers.GetWatcher<Model>().Observable.Returns(_local);
        _readModelWatcherManager.GetWatcher<Model>().Observable.Returns(_server);
        _reducerObservers.ClearReceivedCalls();
        _readModelWatcherManager.ClearReceivedCalls();
    }

    protected void Watch() => _subscription = _readModels.Watch<Model>().Subscribe(_changes.Add);

    protected void SendLocalChanges()
    {
        _local.OnNext(new("test-namespace", "key", _model, false));
        _local.OnNext(new("test-namespace", "key", default, true));
    }

    void Destroy()
    {
        _subscription?.Dispose();
        _local.Dispose();
        _server.Dispose();
    }

    public record Model(string Value);
}
