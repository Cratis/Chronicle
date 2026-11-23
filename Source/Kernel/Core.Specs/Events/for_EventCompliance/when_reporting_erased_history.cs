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

public class when_reporting_erased_history : Specification
{
    EventCompliance _compliance;
    JsonSchema _schema;
    AppendedEvent _stored;
    AppendedEvent[] _result;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var content = await manager.Apply("store", "tenant", _schema, "subject", new JsonObject { ["name"] = "private" });
        var context = EventContext.Empty with { EventStore = "store", Namespace = "tenant", Subject = "subject", EventType = new("person", 1) };
        _stored = new(context, converter.ToExpandoObject(content, _schema))
        {
            OriginalContent = content.ToJsonString(),
            GenerationalContent = new Dictionary<int, string> { [1] = content.ToJsonString() },
            Revisions = [new(1, context.CorrelationId, [], context.CausedBy, context.Occurred, content.ToJsonString())]
        };
        _compliance = new(manager, converter, new OwnGenerationSchemaCache());
        await keys.RecordErasureFor("store", "tenant", "subject");
        await keys.DeleteFor("store", "tenant", "subject");
    }

    async Task Because() => _result = await _compliance.ReleaseReportingErasure([_stored], new Dictionary<EventType, EventTypeSchema>
    {
        [_stored.Context.EventType] = new(_stored.Context.EventType, EventTypeOwner.Client, EventTypeSource.Code, _schema)
    });

    [Fact] void should_report_only_current_content_paths() => _result[0].ErasedProperties.ShouldEqual<IEnumerable<string>>(["name"]);
    [Fact] void should_empty_protected_original_content() => JsonNode.Parse(_result[0].OriginalContent)!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_empty_protected_generational_content() => JsonNode.Parse(_result[0].GenerationalContent[1])!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_empty_protected_revision_content() => JsonNode.Parse(_result[0].Revisions.Single().Content)!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
}
