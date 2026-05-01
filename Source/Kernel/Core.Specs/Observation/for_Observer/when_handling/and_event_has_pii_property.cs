// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_event_has_pii_property : given.an_observer_with_subscription_for_specific_event_type
{
    IEnumerable<AppendedEvent> _receivedEvents = [];
    ExpandoObject _decryptedContent;
    EventTypeSchema _schema;

    void Establish()
    {
        _decryptedContent = new ExpandoObject();
        ((IDictionary<string, object?>)_decryptedContent)["ssn"] = "123-45-6789";

        _schema = new EventTypeSchema(
            event_type,
            EventTypeOwner.Client,
            EventTypeSource.Code,
            new JsonSchema());

        _eventTypesStorage.HasFor(event_type.Id, event_type.Generation).Returns(true);
        _eventTypesStorage.GetFor(event_type.Id, event_type.Generation).Returns(_schema);

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());
        _complianceManager.Release(
            Arg.Any<Concepts.EventStoreName>(),
            Arg.Any<Concepts.EventStoreNamespaceName>(),
            Arg.Any<JsonSchema>(),
            Arg.Any<string>(),
            Arg.Any<JsonObject>())
            .Returns(new JsonObject());
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(_decryptedContent);

        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(callInfo =>
            {
                _receivedEvents = callInfo.ArgAt<IEnumerable<AppendedEvent>>(1);
                return ObserverSubscriberResult.Ok(42UL);
            });
    }

    async Task Because() => await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact]
    void should_call_compliance_manager_release() => _complianceManager.Received(1).Release(
        Arg.Any<Concepts.EventStoreName>(),
        Arg.Any<Concepts.EventStoreNamespaceName>(),
        Arg.Any<JsonSchema>(),
        Arg.Any<string>(),
        Arg.Any<JsonObject>());
    [Fact] void should_deliver_decrypted_content_to_subscriber() => _receivedEvents.First().Content.ShouldEqual(_decryptedContent);
}
