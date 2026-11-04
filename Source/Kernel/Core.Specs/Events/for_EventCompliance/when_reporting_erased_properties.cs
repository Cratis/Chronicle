// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.for_EventCompliance;

public class when_reporting_erased_properties : Specification
{
    EventCompliance _compliance;
    JsonSchema _schema;
    AppendedEvent _stored;
    AppendedEvent _result;
    AppendedEvent _legacy;
    AppendedEvent[] _skipped;
    AppendedEvent[] _independent;
    AppendedEvent _other;
    JsonSchema _otherSchema;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var content = await manager.Apply("store", "tenant", _schema, "subject", new JsonObject { ["name"] = "private" });
        _stored = new(EventContext.Empty with { EventStore = "store", Namespace = "tenant", Subject = "subject" }, converter.ToExpandoObject(content, _schema))
        {
            OriginalContent = "original ciphertext",
            Revisions = [new(2, EventContext.Empty.CorrelationId, [], EventContext.Empty.CausedBy, DateTimeOffset.UtcNow, "revision ciphertext")],
            GenerationalContent = new Dictionary<int, string> { [1] = "generation ciphertext" }
        };
        _otherSchema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"count":{"type":"integer","compliance":[{"metadataType":"PII","details":""}]}}}""");
        var otherContent = await manager.Apply("store", "tenant", _otherSchema, "subject", new JsonObject { ["count"] = 42 });
        _other = new(_stored.Context with { EventType = new("counter", 1) }, converter.ToExpandoObject(otherContent, _otherSchema));
        _compliance = new(manager, converter, new OwnGenerationSchemaCache());
        await keys.RecordErasureFor("store", "tenant", "subject");
        await keys.DeleteFor("store", "tenant", "subject");
    }

    async Task Because()
    {
        _result = await _compliance.ReleaseReportingErasure(_stored, _schema);
        _legacy = await _compliance.Release(_stored, _schema);
        _independent = await _compliance.ReleaseReportingErasure([_stored, _other], new Dictionary<EventType, EventTypeSchema>
        {
            [_stored.Context.EventType] = new(_stored.Context.EventType, EventTypeOwner.Client, EventTypeSource.Code, _schema),
            [_other.Context.EventType] = new(_other.Context.EventType, EventTypeOwner.Client, EventTypeSource.Code, _otherSchema)
        });
        _skipped = await _compliance.ReleaseReportingErasure(
            [_stored with { Context = _stored.Context with { EventType = new("missing", 1) } }, _stored with { Context = _stored.Context with { Subject = null! } }, _stored with { Context = _stored.Context with { EventType = new("unmarked", 1) } }],
            new Dictionary<EventType, EventTypeSchema>
            {
                [_stored.Context.EventType] = new(_stored.Context.EventType, EventTypeOwner.Client, EventTypeSource.Code, _schema),
                [new("unmarked", 1)] = new(new("unmarked", 1), EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema())
            });
    }

    [Fact] void should_use_each_events_schema() => _independent[1].ErasedProperties.ShouldEqual<IEnumerable<string>>(["count"]);
    [Fact] void should_keep_event_path_lists_independent() => _independent[0].ErasedProperties.ShouldEqual<IEnumerable<string>>(["name"]);
    [Fact] void should_preserve_revision_metadata() => _result.Revisions.ShouldEqual(_stored.Revisions);
    [Fact] void should_report_current_content_paths() => _result.ErasedProperties.ShouldEqual<IEnumerable<string>>(["name"]);
    [Fact] void should_keep_existing_release_unannotated() => _legacy.ErasedProperties.ShouldBeEmpty();
    [Fact] void should_keep_existing_content_fallbacks() => ((IDictionary<string, object?>)_result.Content)["name"].ShouldEqual(((IDictionary<string, object?>)_legacy.Content)["name"]);
    [Fact] void should_preserve_context() => _result.Context.ShouldEqual(_stored.Context);
    [Fact] void should_preserve_original_content() => _result.OriginalContent.ShouldEqual(_stored.OriginalContent);
    [Fact] void should_preserve_generational_content() => _result.GenerationalContent.ShouldEqual(_stored.GenerationalContent);
    [Fact] void should_not_mutate_stored_content() => ((IDictionary<string, object?>)_stored.Content)["name"].ShouldNotEqual(string.Empty);
    [Fact] void should_not_annotate_skipped_events() => _skipped.All(_ => _.ErasedProperties.Count == 0).ShouldBeTrue();
}
