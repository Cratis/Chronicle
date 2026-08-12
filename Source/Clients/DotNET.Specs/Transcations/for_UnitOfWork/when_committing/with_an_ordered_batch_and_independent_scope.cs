// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_an_ordered_batch_and_independent_scope : given.a_unit_of_work
{
    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;
    EventSourceId _scopeLabel;
    EventForEventSourceId _firstEvent;
    EventForEventSourceId _secondEvent;
    EventForEventSourceId _thirdEvent;
    ConcurrencyScope _scope;
    List<EventForEventSourceId> _events;
    List<KeyValuePair<EventSourceId, ConcurrencyScope>> _scopes;
    EventForEventSourceId[] _committedEvents;

    protected override AppendManyResult GetAppendResult() => new()
    {
        CorrelationId = _correlationId,
    };

    void Establish()
    {
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();
        _scopeLabel = EventSourceId.New();
        _firstEvent = new(_firstEventSourceId, new FirstEvent(), new Causation(DateTimeOffset.UtcNow, "first", new Dictionary<string, string>()))
        {
            EventStreamType = "first-stream",
            EventStreamId = "first-stream-id",
            EventSourceType = "first-source",
            Tags = ["first-tag"],
            Occurred = DateTimeOffset.UtcNow.AddMinutes(-3),
            Subject = "first-subject"
        };
        _secondEvent = new(_secondEventSourceId, new SecondEvent());
        _thirdEvent = new(_firstEventSourceId, new ThirdEvent());
        _scope = new ConcurrencyScope(42UL, EventTypes: []);
        _events = [_firstEvent, _secondEvent, _thirdEvent];
        _scopes = [new(_scopeLabel, _scope)];

        _unitOfWork.AddEvents(EventSequenceId.Log, _events, _scopes);

        _events.Reverse();
        _scopes.Clear();
    }

    async Task Because()
    {
        await _unitOfWork.Commit();
        _committedEvents = _eventsAppended.ToArray();
    }

    [Fact] void should_commit_the_first_event_first() => _committedEvents[0].ShouldEqual(_firstEvent);
    [Fact] void should_commit_the_second_event_second() => _committedEvents[1].ShouldEqual(_secondEvent);
    [Fact] void should_commit_the_third_event_third() => _committedEvents[2].ShouldEqual(_thirdEvent);
    [Fact] void should_keep_interleaved_events_for_the_first_source_in_their_global_positions() => _committedEvents.Select(_ => _.EventSourceId).ShouldContainOnly(_firstEventSourceId, _secondEventSourceId, _firstEventSourceId);
    [Fact] void should_preserve_the_independent_scope_revision() => _concurrencyScopesAppended[_scopeLabel].SequenceNumber.ShouldEqual(_scope.SequenceNumber);
    [Fact] void should_preserve_the_independent_scope_event_types() => _concurrencyScopesAppended[_scopeLabel].EventTypes.ShouldBeEmpty();
    [Fact] void should_not_require_the_scope_label_to_be_an_event_target() => _committedEvents.Any(_ => _.EventSourceId == _scopeLabel).ShouldBeFalse();
    [Fact] void should_have_materialized_the_events_before_the_caller_mutated_them() => _committedEvents.Length.ShouldEqual(3);
    [Fact] void should_have_materialized_the_scopes_before_the_caller_mutated_them() => _concurrencyScopesAppended.Count.ShouldEqual(1);
    [Fact] void should_append_the_batch_once() => _eventSequence.Received(1).AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());

    record FirstEvent();
    record SecondEvent();
    record ThirdEvent();
}
