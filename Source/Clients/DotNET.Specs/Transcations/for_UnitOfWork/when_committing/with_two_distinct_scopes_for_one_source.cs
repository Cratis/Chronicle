// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_two_distinct_scopes_for_one_source : given.a_unit_of_work
{
    EventSourceId _eventSourceId;
    IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> _scopesSent;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Do<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>(_ => _scopesSent = _.ToArray()))
            .Returns(_appendResult);
        _unitOfWork.AddEvent(EventSequenceId.Log, _eventSourceId, new object(), Causation.Unknown(), concurrencyScope: new ConcurrencyScope(42UL, _eventSourceId, EventTypes: [new EventType("first", 1)]));
        _unitOfWork.AddEvent(EventSequenceId.Log, _eventSourceId, new object(), Causation.Unknown(), concurrencyScope: new ConcurrencyScope(43UL, _eventSourceId, EventTypes: [new EventType("second", 1)]));
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_send_both_scopes_as_pairs() => _scopesSent.Count().ShouldEqual(2);
    [Fact] void should_send_both_scopes_for_the_same_source() => _scopesSent.Select(_ => _.Key).ShouldContainOnly(_eventSourceId, _eventSourceId);
    [Fact] void should_not_use_the_named_tags_overload() => _eventSequence.DidNotReceive().AppendManyWithNamedTags(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<IEnumerable<NamedTag>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>());
}
