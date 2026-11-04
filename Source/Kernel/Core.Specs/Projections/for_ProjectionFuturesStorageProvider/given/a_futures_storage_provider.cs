// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionFuturesStorageProvider.given;

public class a_futures_storage_provider : Specification
{
    protected ProjectionFuturesStorageProvider _provider;
    protected IProjectionFuturesStorage _futuresStorage;
    protected GrainId _firstId;
    protected GrainId _secondId;
    protected GrainState<ProjectionFuturesState> _first;
    protected GrainState<ProjectionFuturesState> _second;
    protected ProjectionFuture _future;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _futuresStorage = Substitute.For<IProjectionFuturesStorage>();
        storage.GetEventStore("store").Returns(eventStore);
        eventStore.GetNamespace("tenant").Returns(namespaceStorage);
        namespaceStorage.ProjectionFutures.Returns(_futuresStorage);
        _provider = new(storage);
        _firstId = GrainId.Create("projection-futures", new ProjectionFuturesKey("first", "store", "tenant").ToString());
        _secondId = GrainId.Create("projection-futures", new ProjectionFuturesKey("second", "store", "tenant").ToString());
        _first = new() { State = new() };
        _second = new() { State = new() };
        _future = new(
            ProjectionFutureId.New(),
            "first",
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("ChildAdded", 1), 42),
            PropertyPath.Root,
            new PropertyPath("children"),
            new PropertyPath("childId"),
            new PropertyPath("id"),
            new Key("parent", ArrayIndexers.NoIndexers),
            DateTimeOffset.UtcNow);
    }

    protected void AddFuture(ProjectionFuturesState state)
    {
        state.Futures.Add(_future);
        state.AddedFutures.Add(_future);
        state.ResolvedFutures.Add(_future);
    }
}
