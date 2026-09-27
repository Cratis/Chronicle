// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

public class when_building_a_redaction_update_for_a_revised_event : Specification
{
    const string Secret = "sensitive-revised-payload";
    BsonDocument _changes;
    string _contentField;
    string _contentHashesField;
    string _revisionsField;

    void Because()
    {
        var revision = new EventRevision(
            EventTypeGeneration.First,
            CorrelationId.New(),
            [],
            IdentityId.NotSet,
            DateTimeOffset.UtcNow,
            new Dictionary<string, BsonDocument> { ["1"] = BsonDocument.Parse($"{{\"secret\":\"{Secret}\"}}") },
            new Dictionary<string, string> { ["1"] = "revised-payload-hash" });
        var original = new Event(
            1,
            CorrelationId.New(),
            [new Causation(DateTimeOffset.UtcNow, "command", new Dictionary<string, string> { ["apiKey"] = Secret })],
            [IdentityId.NotSet],
            "original-type",
            DateTimeOffset.UtcNow,
            EventSourceType.Default,
            "source",
            EventStreamType.All,
            EventStreamId.Default,
            [],
            new Dictionary<string, BsonDocument> { ["1"] = BsonDocument.Parse("{\"original\":\"payload\"}") },
            new Dictionary<string, string> { ["1"] = "original-payload-hash" },
            [revision]);
        var update = EventSequenceStorage.CreateRedactionUpdateModelFor(original, "reason", CorrelationId.New(), [], [], DateTimeOffset.UtcNow, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var registry = BsonSerializer.SerializerRegistry;
        _changes = update.Update.Render(new RenderArgs<Event>(registry.GetSerializer<Event>(), registry))["$set"].AsBsonDocument;
        var classMap = BsonClassMap.LookupClassMap(typeof(Event));
        _contentField = classMap.GetMemberMap(nameof(Event.Content)).ElementName;
        _contentHashesField = classMap.GetMemberMap(nameof(Event.ContentHashes)).ElementName;
        _revisionsField = classMap.GetMemberMap(nameof(Event.Revisions)).ElementName;
    }

    [Fact] void should_replace_the_content_without_the_revised_payload() => _changes.ToJson().ShouldNotContain(Secret);
    [Fact] void should_keep_only_causation_type_and_time_in_the_content()
    {
        var cause = _changes[_contentField]["1"]["causation"][0].AsBsonDocument;
        cause["type"].AsString.ShouldEqual("command");
        cause.Contains("occurred").ShouldBeTrue();
        cause.Contains("properties").ShouldBeFalse();
    }
    [Fact] void should_clear_the_revision_history() => _changes[_revisionsField].AsBsonArray.ShouldBeEmpty();
    [Fact] void should_not_retain_the_original_or_revised_hash() => _changes.ToJson().ShouldNotContain("payload-hash");
    [Fact] void should_clear_the_content_hashes() => _changes[_contentHashesField].AsBsonDocument.ElementCount.ShouldEqual(0);
}
