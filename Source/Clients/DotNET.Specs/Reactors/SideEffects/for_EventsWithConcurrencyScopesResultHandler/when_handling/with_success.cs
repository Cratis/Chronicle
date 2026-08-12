// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsWithConcurrencyScopesResultHandler.when_handling;

public class with_success : Specification
{
    Result<ReactorSideEffectFailure> _result;
    IEventLog _eventLog;
    IEventStore _eventStore;
    EventForEventSourceId _firstEvent;
    EventForEventSourceId _secondEvent;
    EventSourceId _firstScopeKey;
    EventSourceId _secondScopeKey;
    ConcurrencyScope _firstScope;
    ConcurrencyScope _secondScope;
    IEnumerable<EventForEventSourceId> _appendedEvents;
    IDictionary<EventSourceId, ConcurrencyScope> _appendedScopes;

    void Establish()
    {
        _firstEvent = new(EventSourceId.New(), new object());
        _secondEvent = new(EventSourceId.New(), new object());
        _firstScopeKey = _firstEvent.EventSourceId;
        _secondScopeKey = _secondEvent.EventSourceId;
        _firstScope = new(new(42), EventSourceId: _firstEvent.EventSourceId);
        _secondScope = ConcurrencyScope.NotSet;

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(
                Arg.Any<IEnumerable<EventForEventSourceId>>(),
                concurrencyScopes: Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .ReturnsForAnyArgs(callInfo =>
            {
                _appendedEvents = callInfo.ArgAt<IEnumerable<EventForEventSourceId>>(0);
                _appendedScopes = callInfo.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3);
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First, new(1)]);
            });
    }

    async Task Because() => _result = await new EventsWithConcurrencyScopesResultHandler().Handle(
        new(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        _eventStore,
        new EventsWithConcurrencyScopes(
            [_firstEvent, _secondEvent],
            [
                new(_firstScopeKey, _firstScope),
                new(_secondScopeKey, _secondScope)
            ]));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_submit_the_events_as_one_append() => _eventLog.Received(1).AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>());
    [Fact] void should_preserve_the_first_event_position() => _appendedEvents.First().ShouldEqual(_firstEvent);
    [Fact] void should_preserve_the_second_event_position() => _appendedEvents.Last().ShouldEqual(_secondEvent);
    [Fact] void should_pass_every_scope() => _appendedScopes.Count.ShouldEqual(2);
    [Fact] void should_pass_the_first_scope_exactly() => _appendedScopes[_firstScopeKey].ShouldEqual(_firstScope);
    [Fact] void should_pass_the_not_set_scope_exactly() => _appendedScopes[_secondScopeKey].ShouldEqual(_secondScope);
    [Fact] void should_not_append_events_individually() =>
        _eventLog.DidNotReceiveWithAnyArgs().Append(default!, default!, default, default, default, default, default, default, default, default);
}
