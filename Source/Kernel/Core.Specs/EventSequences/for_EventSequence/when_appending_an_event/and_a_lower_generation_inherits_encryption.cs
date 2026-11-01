// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_a_lower_generation_inherits_encryption : given.an_event_sequence_with_inherited_protection
{
    const string Marker = "inherited-encrypted-marker";
    AppendResult _result;

    protected override string SecondSchemaJson => """{"type":"object","properties":{"name":{"type":"string","security":[{"metadataType":"EncryptedSubject","details":""}]}}}""";

    async Task Because() => _result = await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, Plaintext(Marker), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None, subject: new Subject(SubjectIdentifier));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_protect_every_copy_exactly_once() => await AssertProtectedOnce();
    [Fact] void should_hash_the_exact_stored_content() => AssertHashes();

    [Fact]
    async Task should_release_the_appended_copy_through_central_compliance_after_pii_erasure()
    {
        await _keys.DeleteFor(_eventStore, _namespace, SubjectIdentifier);
        var stored = StoredEvents().Single();
        var ownSchema = _definition.Generations.Single(_ => _.Generation == _eventType.Generation).Schema;
        var appended = new AppendedEvent(stored.Context with { Subject = SubjectIdentifier }, _converter.ToExpandoObject(JsonNode.Parse(stored.GenerationalContent![1])!.AsObject(), ownSchema));
        var compliance = new EventCompliance(_metadataManager, _converter, _schemaCache);
        var released = await compliance.Release(appended, ownSchema);
        _converter.ToJsonObject(released.Content, ownSchema)["name"]!.GetValue<string>().ShouldEqual(Marker);
    }
}
