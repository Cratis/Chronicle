// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences.Migrations;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

// Erasure facts deliberately delete the key; each fact needs its own subject state.
[PerFactSpecificationLifecycle]
public class and_nested_and_array_values_inherit_protection : given.an_event_sequence_with_inherited_protection
{
    const string Marker = "nested-array-inherited-marker";
    AppendResult _result;

    protected override string FirstSchemaJson => """{"type":"object","properties":{"person":{"type":"object","properties":{"name":{"type":"string"}}},"names":{"type":"array","items":{"type":"string"}}}}""";
    protected override string SecondSchemaJson => """{"type":"object","properties":{"person":{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}},"names":{"type":"array","items":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}}""";

    protected override JsonObject CreatePlaintext(string marker) => new()
    {
        ["person"] = new JsonObject { ["name"] = marker },
        ["names"] = new JsonArray(marker, marker + "-second")
    };

    async Task Because() => _result = await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, Plaintext(Marker), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None, subject: new Subject(SubjectIdentifier));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_hash_the_exact_stored_content() => AssertHashes();

    [Fact]
    async Task should_protect_each_leaf_once_in_every_copy()
    {
        foreach (var generation in _storedEvents.Single().GenerationalContent)
        {
            var schema = GenerationProtectionSchemas.For(_definition, generation.Key);
            var content = _converter.ToJsonObject(generation.Value, schema);
            content.ToJsonString().ShouldNotContain(Marker);
            var released = await _metadataManager.ReleaseStrict(EventStore, EventStoreNamespace, schema, SubjectIdentifier, content);
            released.ToJsonString().ShouldEqual(_plaintextInputs.Single().ToJsonString());
        }
    }

    [Fact]
    async Task should_remove_every_marker_after_erasure()
    {
        await _keys.DeleteFor(EventStore, EventStoreNamespace, SubjectIdentifier);
        foreach (var generation in _storedEvents.Single().GenerationalContent)
        {
            var schema = GenerationProtectionSchemas.For(_definition, generation.Key);
            var released = await _metadataManager.Release(EventStore, EventStoreNamespace, schema, SubjectIdentifier, _converter.ToJsonObject(generation.Value, schema));
            released.ToJsonString().ShouldNotContain(Marker);
        }
    }
}
