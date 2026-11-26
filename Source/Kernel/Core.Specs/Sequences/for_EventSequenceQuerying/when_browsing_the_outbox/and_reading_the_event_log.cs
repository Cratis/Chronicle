// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.when_browsing_the_outbox;

public class and_reading_the_event_log : given.an_outbox_with_a_mixed_catalog
{
    IEventSequenceStorage _log;

    void Establish()
    {
        _log = _storage.GetEventStore((EventStoreName)"store").GetNamespace((EventStoreNamespaceName)"default").GetEventSequence((Concepts.EventSequences.EventSequenceId)"event-log");
        _log.GetCountMatching(Arg.Any<EventSequenceQueryCriteria>()).Returns((Concepts.Events.EventCount)1UL);
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(false);
        _log.GetPage(Arg.Any<EventSequenceQueryCriteria>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventSequenceQuerySort>(), Arg.Any<CancellationToken>()).Returns(cursor);
    }

    async Task Because() => await EventSequenceQuerying.QueryPage(_storage, _compliance, "store", "default", "event-log", EventSequenceQueryCriteria.Empty, 0, 10, EventSequenceQuerySort.Default);

    [Fact] void should_not_restrict_event_types() => _log.Received(1).GetCountMatching(Arg.Is<EventSequenceQueryCriteria>(_ => !_.HasEventTypes));
}
