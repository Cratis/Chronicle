// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_event_type_schema_without_pii_is_loaded_during_handling : given.an_observer_with_subscription_for_specific_event_type
{
    IEnumerable<AppendedEvent> _receivedEvents = [];
    ExpandoObject _originalContent;
    JsonSchema _schema;

    void Establish()
    {
        _schema = new JsonSchema();
        _schema.Properties["value"] = new JsonSchemaProperty();

        _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns([new EventTypeSchema(event_type, EventTypeOwner.Client, EventTypeSource.Code, _schema)]);

        _originalContent = new ExpandoObject();
        ((IDictionary<string, object?>)_originalContent)["value"] = 42;

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

    [Fact] void should_fetch_schema_for_the_handled_event_type() => _eventTypesStorage.Received(1).GetFor(Arg.Any<IEnumerable<EventType>>());

    [Fact]
    void should_not_call_compliance_manager_release() => _complianceManager.DidNotReceive().Release(
        Arg.Any<Concepts.EventStoreName>(),
        Arg.Any<Concepts.EventStoreNamespaceName>(),
        Arg.Any<JsonSchema>(),
        Arg.Any<string>(),
        Arg.Any<JsonObject>());

    [Fact] void should_pass_event_content_through_unchanged() => _receivedEvents.First().Content.ShouldEqual(_originalContent);
}
