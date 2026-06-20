// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_reactor_returns_mixed_side_effects : Specification
{
    const string MemberId = "member-77";

    ReactorScenario<MixedReservationReactor> _scenario;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventSourceId _triggeringEventSourceId;
    IEnumerable<EventForEventSourceId> _appended;

    void Establish()
    {
        _triggeringEventSourceId = EventSourceId.New();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>())
            .ReturnsForAnyArgs(callInfo =>
            {
                _appended = callInfo.Arg<IEnumerable<EventForEventSourceId>>();
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First, new EventSequenceNumber(1)]);
            });

        var eventTypes = Defaults.Instance.EventTypes;
        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>(
        [
            new EventResultHandler(eventTypes),
            new EventsResultHandler(eventTypes),
            new EventForEventSourceIdResultHandler(),
            new EventsForEventSourceIdResultHandler(),
            new MixedSideEffectsResultHandler(eventTypes)
        ]));

        _scenario = new ReactorScenario<MixedReservationReactor>(sideEffectHandlers: sideEffectHandlers, eventStore: _eventStore);
    }

    async Task Because() =>
        await _scenario.Given.ForEventSource(_triggeringEventSourceId).Events(new ReservationMade(MemberId));

    [Fact] void should_append_both_side_effects() => _appended.Count().ShouldEqual(2);
    [Fact] void should_append_the_bare_event_to_the_triggering_event_source_id() => _appended.First().EventSourceId.ShouldEqual(_triggeringEventSourceId);
    [Fact] void should_append_the_event_for_event_source_id_to_the_member() => _appended.Last().EventSourceId.Value.ShouldEqual(MemberId);
}
