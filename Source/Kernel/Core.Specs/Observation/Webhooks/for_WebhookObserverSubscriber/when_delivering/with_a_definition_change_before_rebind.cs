// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_a_definition_change_before_rebind : given.a_webhook_delivery
{
    void Establish()
    {
        var currentPin = new EventType(_pin.Id, 1);
        _webhookDefinition = _webhookDefinition with { EventTypes = [currentPin] };
        _eventTypes.GetFor(currentPin.Id, currentPin.Generation).Returns(new EventTypeSchema(currentPin, EventTypeOwner.Client, EventTypeSource.Code, _firstSchema));
        _events = [_events[0] with { Context = _events[0].Context with { EventType = _pin, Hash = "second-hash" } }];
    }

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));
    [Fact] void should_deliver_instead_of_dropping_the_mismatch() => _receiver.Payloads.Count.ShouldEqual(1);
    [Fact] void should_deliver_the_current_generation() => JsonNode.Parse(_receiver.Payloads.Single())!["events"]![0]!["context"]!["eventType"]!["generation"]!.GetValue<uint>().ShouldEqual(1u);
    [Fact] void should_release_the_current_pins_content() => JsonNode.Parse(_receiver.Payloads.Single())!["events"]![0]!["content"]!["name"]!.GetValue<string>().ShouldEqual("Ada Lovelace");
    [Fact] void should_acknowledge_only_after_delivery() => _deliveryResult.LastSuccessfulObservation.Value.ShouldEqual(42ul);
}
