// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences.Migrations;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

[PerFactSpecificationLifecycle]
public class and_array_object_items_inherit_protection : given.an_event_sequence_with_inherited_protection
{
    const string Marker = "array-object-inherited-marker";
    AppendResult _result;

    protected override string FirstSchemaJson => """{"type":"object","properties":{"people":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"}}}}}}""";
    protected override string SecondSchemaJson => """{"type":"object","properties":{"people":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}}}}""";
    protected override JsonObject CreatePlaintext(string marker) => new()
    {
        ["people"] = new JsonArray(new JsonObject { ["name"] = marker }, new JsonObject { ["name"] = marker + "-second" })
    };

    async Task Because() => _result = await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, Plaintext(Marker), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None, subject: new Subject(SubjectIdentifier));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_hash_the_exact_stored_content() => AssertHashes();

    [Fact]
    async Task should_release_every_array_object_leaf_exactly_once_in_every_generation()
    {
        foreach (var (generation, content) in _storedEvents.Single().GenerationalContent)
        {
            var schema = GenerationProtectionSchemas.For(_definition, generation);
            var json = _converter.ToJsonObject(content, schema);
            json.ToJsonString().ShouldNotContain(Marker);
            var released = await _metadataManager.ReleaseStrict(EventStore, EventStoreNamespace, schema, SubjectIdentifier, json);
            JsonNode.DeepEquals(released, _plaintextInputs.Single()).ShouldBeTrue();
        }
    }

    [Fact]
    async Task should_not_reveal_array_object_leaves_after_erasure()
    {
        await _keys.RecordErasureFor(EventStore, EventStoreNamespace, SubjectIdentifier);
        await _keys.DeleteFor(EventStore, EventStoreNamespace, SubjectIdentifier);
        foreach (var (generation, content) in _storedEvents.Single().GenerationalContent)
        {
            var schema = GenerationProtectionSchemas.For(_definition, generation);
            var released = await _metadataManager.Release(EventStore, EventStoreNamespace, schema, SubjectIdentifier, _converter.ToJsonObject(content, schema));
            released.ToJsonString().ShouldNotContain(Marker);
        }
    }
}
