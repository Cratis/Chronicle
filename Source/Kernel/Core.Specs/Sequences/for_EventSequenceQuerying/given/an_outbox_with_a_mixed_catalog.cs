// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.given;

public class an_outbox_with_a_mixed_catalog : Specification
{
    protected IStorage _storage;
    protected IEventSequenceStorage _sequence;
    protected IEventCompliance _compliance;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var store = _storage.GetEventStore((EventStoreName)"store");
        store.EventTypes.GetLatestForAllEventTypes().Returns(
        [
            Registration("public", EventTypeVisibility.Public),
            Registration("private", EventTypeVisibility.Private)
        ]);
        _sequence = store.GetNamespace((EventStoreNamespaceName)"default").GetEventSequence((Concepts.EventSequences.EventSequenceId)"outbox");
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(false);
        _sequence.GetCountMatching(Arg.Any<EventSequenceQueryCriteria>()).Returns((EventCount)5UL);
        _sequence.GetPage(Arg.Any<EventSequenceQueryCriteria>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventSequenceQuerySort>(), Arg.Any<CancellationToken>()).Returns(cursor);
        _sequence.GetHistogram(Arg.Any<HistogramResolution>(), Arg.Any<EventSequenceQueryCriteria>()).Returns([new HistogramBucket(DateTimeOffset.UnixEpoch, 5)]);
        _compliance = Substitute.For<IEventCompliance>();
    }

    protected static EventTypeSchema Registration(string id, EventTypeVisibility visibility) =>
        new(new Concepts.Events.EventType(id, EventTypeGeneration.First), EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromJson("{\"type\":\"object\"}"), visibility);

    protected static bool OnlyPublic(EventSequenceQueryCriteria criteria) => criteria.EventTypes!.Select(_ => _.Id.Value).SequenceEqual(["public"]);
}
