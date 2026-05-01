// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
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

        var appendedEvent = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL) with
        {
            Context = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL).Context,
            Content = _originalContent
        };

        // HasFor returns false (default set in an_observer Establish)
        _eventTypesStorage.HasFor(event_type.Id, event_type.Generation).Returns(false);

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

    [Fact] void should_not_call_compliance_manager() => _complianceManager.DidNotReceive().Release(
        Arg.Any<Cratis.Chronicle.Concepts.EventStoreName>(),
        Arg.Any<Cratis.Chronicle.Concepts.EventStoreNamespaceName>(),
        Arg.Any<Cratis.Chronicle.Schemas.JsonSchema>(),
        Arg.Any<string>(),
        Arg.Any<System.Text.Json.Nodes.JsonObject>());
    [Fact] void should_pass_event_through_unchanged() => _receivedEvents.First().Content.ShouldEqual(_originalContent);
}
