// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_protected_type_is_ambiguous : given.a_protected_event
{
    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":["string","number"]}}} """);
        schema.Properties["value"].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new ComplianceSchemaMetadata[] { new(ComplianceMetadataType.PII.Value, string.Empty) }
        };
        _storage.GetEventStore("store").EventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, schema));
        var key = await _keys.TryGetFor("store", "tenant", _stored.Context.Subject.Value);
        var encrypted = new JsonObject { ["value"] = ProtectedValueCodec.Encrypt(_encryption, key!, JsonValue.Create(42)) };
        _stored = _stored with { GenerationalContent = new Dictionary<int, string> { [1] = encrypted.ToJsonString() } };
        _command = _command with { Content = "{\"value\":\"42\"}" };
    }

    async Task Because() => _result = await _command.Handle(_storage, _manager, _converter);

    [Fact] void should_not_report_equality_between_encrypted_number_and_attempted_text() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
