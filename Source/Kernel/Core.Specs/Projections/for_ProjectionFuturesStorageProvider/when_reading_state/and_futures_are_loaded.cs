// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionFuturesStorageProvider.when_reading_state;

public class and_futures_are_loaded : Specification
{
    ProjectionFuturesStorageProvider _provider;
    ProjectionFuturesKey _key;
    IGrainState<ProjectionFuturesState> _state;
    ProjectionFuture[] _futures;
    ProjectionFuture[] _expectedFutures;

    void Establish()
    {
        _key = new ProjectionFuturesKey("test-projection", "event-store", "tenant");
        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        var futuresStorage = Substitute.For<IProjectionFuturesStorage>();
        storage.GetEventStore(_key.EventStore).Returns(eventStore);
        eventStore.GetNamespace(_key.Namespace).Returns(namespaceStorage);
        namespaceStorage.ProjectionFutures.Returns(futuresStorage);
        _provider = new ProjectionFuturesStorageProvider(storage);
        _state = new GrainState<ProjectionFuturesState> { State = new() };

        var @event = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("ChildAdded", 1), 42);
        @event = @event with { Context = @event.Context with { EventSourceId = "child-source", CorrelationId = Guid.NewGuid() } };
        ((IDictionary<string, object?>)@event.Content)["childId"] = "child-key";
        _futures =
        [
            CreateFuture(@event),
            CreateFuture(@event with { Context = @event.Context with { EventStore = "other-store", Namespace = "other-tenant", SequenceNumber = 43 } })
        ];
        _expectedFutures = _futures.Select(future => future with
        {
            Event = future.Event with
            {
                Context = future.Event.Context with { EventStore = _key.EventStore, Namespace = _key.Namespace }
            }
        }).ToArray();
        futuresStorage.GetForProjection(_key.ProjectionId).Returns(Task.FromResult<IEnumerable<ProjectionFuture>>(_futures));
    }

    Task Because() => _provider.ReadStateAsync("state", GrainId.Create("projection-futures", _key.ToString()), _state);

    [Fact] void should_load_every_future() => _state.State.Futures.Count.ShouldEqual(2);
    [Fact] void should_stamp_the_event_store_on_every_future() => _state.State.Futures.Select(future => future.Event.Context.EventStore).ShouldEqual([_key.EventStore, _key.EventStore]);
    [Fact] void should_stamp_the_namespace_on_every_future() => _state.State.Futures.Select(future => future.Event.Context.Namespace).ShouldEqual([_key.Namespace, _key.Namespace]);
    [Fact] void should_preserve_the_rest_of_each_future() => _state.State.Futures.ShouldEqual<ProjectionFuture>(_expectedFutures);
    [Fact] void should_not_mutate_the_stored_event_context() => _futures[0].Event.Context.EventStore.ShouldEqual(EventContext.Empty.EventStore);

    ProjectionFuture CreateFuture(AppendedEvent @event) => new(
        ProjectionFutureId.New(),
        _key.ProjectionId,
        @event,
        PropertyPath.Root,
        new PropertyPath("children"),
        new PropertyPath("childId"),
        new PropertyPath("id"),
        new Key("parent-key", ArrayIndexers.NoIndexers),
        DateTimeOffset.UtcNow);
}
