// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

/// <summary>
/// With the real side-effect handlers the in-process kernel advertises idempotent appends, so the events a reactor returns
/// are appended with keys derived from the delivery and a redelivery of the same occurrence does not append them again.
/// The count is taken from the event log, not from handler calls.
/// </summary>
public class when_a_reactor_delivery_is_redelivered_with_the_real_handlers : Specification
{
    const string MemberId = "member-77";

    EventStoreForTesting _eventStore;
    ReactorScenario<MixedReservationReactor> _scenario;
    RecordedOccurrence _occurrence;
    int _afterFirstDelivery;
    int _afterRedelivery;

    async Task Establish()
    {
        _eventStore = new EventStoreForTesting();
        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>(
        [
            new EventResultHandler(),
            new EventsResultHandler(),
            new EventForEventSourceIdResultHandler(),
            new EventsForEventSourceIdResultHandler(),
            new MixedSideEffectsResultHandler()
        ]));
        _scenario = new ReactorScenario<MixedReservationReactor>(sideEffectHandlers: sideEffectHandlers, eventStore: _eventStore);
        _occurrence = await _scenario.Given.ForEventSource(EventSourceId.New()).History(new ReservationMade(MemberId));
    }

    async Task Because()
    {
        await _scenario.Redeliver(_occurrence);
        _afterFirstDelivery = await CountReturnedEvents();
        await _scenario.Redeliver(_occurrence);
        await _scenario.Redeliver(_occurrence);
        _afterRedelivery = await CountReturnedEvents();
    }

    void Destroy()
    {
        _scenario.Dispose();
        _eventStore.Dispose();
    }

    [Fact] void should_advertise_idempotent_appends() => _eventStore.EventLog.SupportsIdempotentAppends.ShouldBeTrue();
    [Fact] void should_append_every_returned_event_for_the_first_delivery() => _afterFirstDelivery.ShouldEqual(2);
    [Fact] void should_not_append_the_returned_events_again_on_redelivery() => _afterRedelivery.ShouldEqual(2);

    async Task<int> CountReturnedEvents()
    {
        var returned = typeof(MemberActivityRecorded).GetEventType();
        var events = await _eventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        return events.Count(_ => _.Context.EventType.Id == returned.Id);
    }
}
