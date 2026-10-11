// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_the_pinned_generation_is_missing_and_appended_is_known : given.a_release_boundary
{
    void Establish()
    {
        _definition = _definition with { Generations = [.. _definition.Generations, new(3, CreateSchema("newName"))] };
        _event = _event with
        {
            Context = _event.Context with { EventType = new(_pin.Id, 3) },
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"ciphertext\"}", [3] = "{\"newName\":\"wrong-source\"}" }
        };
    }

    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_migrate_the_appended_content() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_not_claim_a_durable_hash() => _result[0].Context.Hash.ShouldEqual(EventHash.NotSet);
    [Fact] async Task should_strictly_release_the_source() => await _metadata.Received(1).ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), _firstSchema, "person", Arg.Any<JsonObject>());
    [Fact] void should_never_access_event_sequence_storage() => _storage.GetEventStore(_event.Context.EventStore).DidNotReceive().GetNamespace(Arg.Any<EventStoreNamespaceName>());
    [Fact] async Task should_not_release_migrated_plaintext_again() => await _metadata.DidNotReceive().Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
}
