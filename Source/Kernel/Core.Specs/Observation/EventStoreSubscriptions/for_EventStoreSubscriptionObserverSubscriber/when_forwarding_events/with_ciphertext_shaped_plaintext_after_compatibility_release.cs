// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_ciphertext_shaped_plaintext_after_compatibility_release : given.a_pinned_subscriber
{
    AppendedEvent _stored;
    int _repeatedReleases;

    async Task Establish()
    {
        _definition = _definition with { GenerationDelivery = EventGenerationDelivery.Compatibility, EventTypes = [] };
        var converter = new ExpandoObjectConverter(Substitute.For<ITypeFormats>());
        _stored = _events[0] with
        {
            Content = converter.ToExpandoObject(new JsonObject { ["message"] = "ciphertext" }, _schema),
            OriginalContent = "{\"message\":\"ciphertext\"}",
            Revisions = [new(1, CorrelationId.NotSet, [], Concepts.Identities.Identity.System, DateTimeOffset.UnixEpoch, "{\"message\":\"ciphertext\"}")]
        };
        _storage.GetEventStore("Admin").GetNamespace(EventStoreNamespaceName.Default).GetEventSequence("outbox")
            .GetEventAt(42UL).Returns(_stored);
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => ReleaseOnce(call.Arg<JsonObject>()));
        _metadata.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => ReleaseOnce(call.Arg<JsonObject>()));
        var schemas = new uint[] { 1, 2 }.Select(generation => new EventType("event-type-1", generation))
            .ToDictionary(type => type, type => new EventTypeSchema(type, EventTypeOwner.Client, EventTypeSource.Code, _schema));
        _events = await new EventCompliance(_metadata, converter, new OwnGenerationSchemaCache()).Release([_stored], schemas);
    }

    async Task Because() => _result = await Forward();

    [Fact] void should_release_for_the_lower_target_generation() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("enc:plaintext");
    [Fact] void should_never_release_plaintext_again() => _repeatedReleases.ShouldEqual(0);
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_acknowledge_the_source_position() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_preserve_stored_history() => _stored.OriginalContent.ShouldEqual("{\"message\":\"ciphertext\"}");
    [Fact] void should_preserve_the_shared_stored_content() => ((IDictionary<string, object?>)_stored.Content)["message"].ShouldEqual("ciphertext");
    [Fact] void should_preserve_the_shared_stored_generation() => _stored.GenerationalContent[2].ShouldEqual("{\"message\":\"new-generation\"}");
    [Fact] void should_preserve_the_shared_stored_revision() => _stored.Revisions.Single().Content.ShouldEqual("{\"message\":\"ciphertext\"}");
    [Fact] void should_release_into_a_new_content_object() => ReferenceEquals(_stored.Content, _events[0].Content).ShouldBeFalse();
    [Fact] void should_release_into_a_new_history_dictionary() => ReferenceEquals(_stored.GenerationalContent, _events[0].GenerationalContent).ShouldBeFalse();

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
