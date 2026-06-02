// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_event_type_schema_is_unknown : given.an_observer_with_subscription_for_specific_event_type
{
    IEnumerable<AppendedEvent> _receivedEvents = [];
    ExpandoObject _originalContent;

    void Establish()
    {
        _originalContent = new ExpandoObject();
        ((IDictionary<string, object?>)_originalContent)["raw"] = "encrypted-data";

        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(callInfo =>
            {
                _receivedEvents = callInfo.ArgAt<IEnumerable<AppendedEvent>>(1);
                return ObserverSubscriberResult.Ok(42UL);
            });
    }

    async Task Because()
    {
        var appendedEvent = new AppendedEvent(
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL).Context,
            _originalContent);
        await _observer.Handle("Something", [appendedEvent]);
    }

    [Fact] void should_call_decrypt_events_on_compliance_helper() =>
        _eventComplianceHelper.Received(1).DecryptEvents(
            Arg.Any<IEnumerable<AppendedEvent>>(),
            Arg.Any<IDictionary<EventType, EventTypeSchema>>());

    [Fact] void should_pass_event_through_unchanged() => _receivedEvents.First().Content.ShouldEqual(_originalContent);
}
