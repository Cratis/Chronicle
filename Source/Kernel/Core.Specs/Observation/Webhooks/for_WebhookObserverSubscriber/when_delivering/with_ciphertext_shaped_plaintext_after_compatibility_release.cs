// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_ciphertext_shaped_plaintext_after_compatibility_release : given.a_webhook_delivery
{
    AppendedEvent[] _stored;
    int _repeatedReleases;

    async Task Establish()
    {
        _events = _events.Select(@event => @event with { Context = @event.Context with { ObservationState = EventObservationState.Initial } }).ToArray();
        _stored = _events.ToArray();
        _context = _context with { UnreleasedEvents = _stored };
        _storage.GetEventStore("store").GetNamespace("namespace").GetEventSequence("log")
            .GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call => _stored.Single(@event => @event.Context.SequenceNumber == call.Arg<EventSequenceNumber>()));
        _silo.StorageManager.GetStorage<WebhookDefinition>(typeof(WebhookObserverSubscriber).FullName).State = _webhookDefinition with
        {
            GenerationDelivery = EventGenerationDelivery.Compatibility,
            EventTypes = [new(_pin.Id, 1)]
        };
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => ReleaseOnce(call.Arg<JsonObject>()));
        _metadata.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => ReleaseOnce(call.Arg<JsonObject>()));
        _events = await new EventCompliance(_metadata, _converter, new OwnGenerationSchemaCache()).Release(_events, _schemas);
        _events = _events.Select(@event => @event with { Context = @event.Context with { ObservationState = EventObservationState.Replay } }).ToArray();
        _context = JsonSerializer.Deserialize<ObserverSubscriberContext>(JsonSerializer.Serialize(_context, _jsonOptions), _jsonOptions)!;
        _storage.GetEventStore("store").GetNamespace("namespace").GetEventSequence("log").ClearReceivedCalls();
    }

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, _context);

    [Fact] void should_deliver_every_payload_slot_without_a_second_release() => _receiver.Payloads.Single().ShouldEqual(Fixture("pinned").Replace("Ada Lovelace", "enc:plaintext").Replace($"\"observationState\":{(int)EventObservationState.Initial}", $"\"observationState\":{(int)EventObservationState.Replay}"));
    [Fact] void should_never_release_plaintext_again() => _repeatedReleases.ShouldEqual(0);
    [Fact] void should_preserve_replay_in_the_http_payload() => JsonNode.Parse(_receiver.Payloads.Single())!["events"]!.AsArray().Select(@event => @event!["context"]!["observationState"]!.GetValue<int>()).ShouldEachConformTo(state => state == (int)EventObservationState.Replay);
    [Fact] void should_not_reload_each_event() => _storage.GetEventStore("store").GetNamespace("namespace").GetEventSequence("log").DidNotReceive().GetEventAt(Arg.Any<EventSequenceNumber>());
    [Fact] void should_acknowledge_the_batch() => _deliveryResult.LastSuccessfulObservation.Value.ShouldEqual(44ul);
    [Fact] void should_preserve_stored_history() => _stored[1].OriginalContent.ShouldEqual("{\"name\":\"original-ciphertext\"}");
    [Fact] void should_preserve_the_shared_stored_content() => ((IDictionary<string, object?>)_stored[0].Content)["name"].ShouldEqual("ciphertext");
    [Fact] void should_preserve_the_shared_stored_generation() => _stored[0].GenerationalContent[1].ShouldEqual("{\"name\":\"ciphertext\"}");
    [Fact] void should_preserve_the_shared_stored_revision() => _stored[1].Revisions.Single().Content.ShouldEqual("{\"name\":\"ciphertext\"}");
    [Fact] void should_release_into_a_new_content_object() => ReferenceEquals(_stored[0].Content, _events[0].Content).ShouldBeFalse();
    [Fact] void should_release_into_a_new_history_dictionary() => ReferenceEquals(_stored[0].GenerationalContent, _events[0].GenerationalContent).ShouldBeFalse();

    JsonObject ReleaseOnce(JsonObject content)
    {
        var released = (JsonObject)content.DeepClone();
        foreach (var property in released.ToArray())
        {
            if (property.Value!.GetValue<string>() == "enc:plaintext")
            {
                _repeatedReleases++;
                throw new InvalidOperationException("Plaintext was released twice");
            }
            released[property.Key] = "enc:plaintext";
        }
        return released;
    }
}
