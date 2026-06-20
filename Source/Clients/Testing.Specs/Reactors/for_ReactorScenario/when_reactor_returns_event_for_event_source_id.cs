// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_reactor_returns_event_for_event_source_id : Specification
{
    const string MemberId = "member-42";

    ReactorScenario<ReservationReactor> _scenario;
    IEventStore _eventStore;
    IEventLog _eventLog;
    IEnumerable<EventForEventSourceId> _appended;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>())
            .ReturnsForAnyArgs(callInfo =>
            {
                _appended = callInfo.Arg<IEnumerable<EventForEventSourceId>>();
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]);
            });

        var eventTypes = Defaults.Instance.EventTypes;
        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>(
        [
            new EventResultHandler(eventTypes),
            new EventsResultHandler(eventTypes),
            new EventForEventSourceIdResultHandler(),
            new EventsForEventSourceIdResultHandler()
        ]));

        _scenario = new ReactorScenario<ReservationReactor>(sideEffectHandlers: sideEffectHandlers, eventStore: _eventStore);
    }

    async Task Because() =>
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ReservationMade(MemberId));

    [Fact] void should_append_a_single_event() => _appended.Count().ShouldEqual(1);
    [Fact] void should_append_to_the_member_event_source_id() => _appended.Single().EventSourceId.Value.ShouldEqual(MemberId);
    [Fact] void should_append_the_member_activity_event() => _appended.Single().Event.ShouldBeOfExactType<MemberActivityRecorded>();
}
