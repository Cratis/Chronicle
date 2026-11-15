// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_the_requested_generation_inherits_pii : given.an_event_sequence_with_inherited_protection
{
    const string Marker = "inherited-revision-marker";
    JsonObject _stored;
    JsonObject _released;
    JsonObject _erased;

    protected virtual Subject? OriginalSubject => (Subject)SubjectIdentifier;

    void Establish() => _eventSequenceStorage.GetEventAt(0UL).Returns(new AppendedEvent(
        EventContext.From(EventStore, EventStoreNamespace, _eventType, EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, 0UL, CorrelationId.New(), subject: OriginalSubject),
        new ExpandoObject()));

    async Task Because()
    {
        await _eventSequence.Revise(0UL, _eventType, Plaintext(Marker), CorrelationId.New(), [], Identity.System);
        var content = (ExpandoObject)_eventSequenceStorage.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IEventSequenceStorage.Revise)).GetArguments()[6]!;
        var schema = GenerationProtectionSchemas.For(_definition, _eventType.Generation);
        var identifier = OriginalSubject?.IsSet == true ? OriginalSubject.Value : _eventSourceId.Value;
        _stored = _converter.ToJsonObject(content, schema);
        _released = await _metadataManager.ReleaseStrict(EventStore, EventStoreNamespace, schema, identifier, _stored);
        await _keys.RecordErasureFor(EventStore, EventStoreNamespace, identifier);
        await _keys.DeleteFor(EventStore, EventStoreNamespace, identifier);
        _erased = await _metadataManager.Release(EventStore, EventStoreNamespace, schema, identifier, _stored);
    }

    [Fact] void should_store_no_personal_plaintext() => _stored.ToJsonString().ShouldNotContain(Marker);
    [Fact] void should_release_once_with_the_original_subject_or_source_fallback() => _released["name"]!.GetValue<string>().ShouldEqual(Marker);
    [Fact] void should_not_reveal_the_revision_after_erasure() => _erased["name"]!.GetValue<string>().ShouldEqual(string.Empty);
}
