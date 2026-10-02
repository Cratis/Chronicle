// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_preserving_main_append_acceptance
{
    [Theory]
    [InlineData("reference")]
    [InlineData("union")]
    [InlineData("conflict")]
    [InlineData("dynamic")]
    [InlineData("cycle")]
    [InlineData("tuple")]
    [InlineData("malformed")]
    public async Task should_not_add_registration_style_refusals_to_previously_accepted_appends(string shape)
    {
        const string Marker = "\"compliance\":[{\"metadataType\":\"PII\",\"details\":\"\"}]";
        var member = shape switch
        {
            "reference" => """{"$ref":"#/$defs/missing"}""",
            "union" => $$"""{"anyOf":[{"type":"string"},{"type":"string",{{Marker}}}]}""",
            "conflict" => $$"""{"allOf":[{"type":"string"},{"type":"integer",{{Marker}}}]}""",
            "dynamic" => $$"""{"type":"object","additionalProperties":{"type":"string",{{Marker}} } }""",
            "tuple" => $$"""{"type":"array","items":[{"type":"string",{{Marker}} }]}""",
            "malformed" => """{"type":"string","compliance":[{"metadataType":"PII"}]}""",
            _ => """{"$ref":"#/$defs/cycle"}"""
        };
        var schema = await JsonSchema.FromJsonAsync($$"""
            {"type":"object","$defs":{"cycle":{"$ref":"#/$defs/cycle"} },
            "properties":{"guard":{"type":"string",{{Marker}} },"value":{{member}} } }
            """);
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var input = JsonNode.Parse("""{"guard":"guard","value":"personal-value"}""")!.AsObject();
        var stored = await manager.Apply("store", "Default", schema, "subject", input);
        var released = await manager.Release("store", "Default", schema, "subject", stored);
        Assert.True(JsonNode.DeepEquals(input, released));
        Assert.True(ProtectedValueCodec.TryDecodeCipherText(encryption, stored["guard"]!.GetValue<string>(), out _));
    }
}
