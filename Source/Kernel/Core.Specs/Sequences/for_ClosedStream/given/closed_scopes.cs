// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Sequences.for_ClosedStream.given;

public class closed_scopes : Specification
{
    protected IStorage _storage;
    protected IQueryContextManager _queryContexts;
    protected ClosedStream[] _rows;

    async Task Establish()
    {
        _storage = Substitute.For<IStorage>();
        _queryContexts = Substitute.For<IQueryContextManager>();
        var store = Substitute.For<IEventStoreStorage>();
        var ns = Substitute.For<IEventStoreNamespaceStorage>();
        var closures = new ClosedStreamsConstraintStorage();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
        store.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(ns);
        ns.GetClosedStreamsConstraints(Arg.Any<EventSequenceId>()).Returns(closures);
        await closures.Close(new(new(EventSourceId: "one"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
        await closures.Close(new(new(EventSourceId: "two"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
        await closures.Close(new(new(EventSourceId: "three"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
    }
}
