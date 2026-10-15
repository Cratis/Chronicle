// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_whole_protected_container_has_nulls_and_undeclared_members : given.a_protected_event
{
    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"object","properties":{"name":{"type":"string"},"optional":{"type":["string","null"]}}}}}""");
        schema.Properties["value"].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new ComplianceSchemaMetadata[] { new(ComplianceMetadataType.PII.Value, string.Empty) }
        };
        _storage.GetEventStore("store").EventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, schema));
        _command = _command with { Content = """{"value":{"name":"private","optional":null,"undeclared":{"amount":0.1234567890123456789012345678,"nil":null}}}""" };
        var key = await _keys.TryGetFor((EventStoreName)"store", (EventStoreNamespaceName)"tenant", "subject");
        var document = JsonNode.Parse(_command.Content)!.AsObject();
        document["value"] = ProtectedValueCodec.Encrypt(_encryption, key!, document["value"]!);
        var content = _converter.ToExpandoObject(document, schema);
        var serialized = _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log").SerializeContentForVerification(content, schema);
        _stored = _stored with { GenerationalContent = new Dictionary<int, string> { [1] = serialized! } };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_compare_the_complete_plaintext_without_schema_trimming() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
