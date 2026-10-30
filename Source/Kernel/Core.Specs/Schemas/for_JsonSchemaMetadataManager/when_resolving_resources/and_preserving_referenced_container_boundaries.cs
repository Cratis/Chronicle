// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_resolving_resources;

public class and_preserving_referenced_container_boundaries
{
    [Theory]
    [InlineData(false, "#/$defs/personal")]
    [InlineData(true, "#/$defs/personal")]
    [InlineData(false, "personal")]
    [InlineData(true, "personal")]
    public async Task should_encrypt_and_report_members_instead_of_the_referenced_container(bool array, string reference)
    {
        var (manager, _, encryption) = CreateManager();
        var (schema, input) = Container(array, reference);
        var stored = await manager.Apply("store", "tenant", schema, "subject", input);
        foreach (var member in Members(stored, array))
        {
            ProtectedValueCodec.TryDecodeCipherText(encryption, member.Value!.GetValue<string>(), out _).ShouldBeTrue();
        }

        var locations = await manager.LocateProtectedValues(schema, "subject", input);
        var reported = await manager.ApplyReportingKeys("store", "tenant", schema, "subject", input);
        var prefix = array ? "value[0]" : "value";
        locations.Select(_ => _.Path).ShouldEqual([$"{prefix}.email", $"{prefix}.name"]);
        reported.Uses.Select(_ => _.Path).ShouldEqual(locations.Select(_ => _.Path));
        foreach (var member in Members(reported.Value, array))
        {
            ProtectedValueCodec.TryDecodeCipherText(encryption, member.Value!.GetValue<string>(), out _).ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData(false, "#/$defs/personal")]
    [InlineData(true, "#/$defs/personal")]
    [InlineData(false, "personal")]
    [InlineData(true, "personal")]
    public async Task should_round_trip_member_ciphertext_through_release_and_comparison(bool array, string reference)
    {
        var (manager, _, encryption) = CreateManager();
        var (schema, input) = Container(array, reference);
        var stored = await manager.Apply("store", "tenant", schema, "subject", input);
        JsonNode.DeepEquals(input, await manager.Release("store", "tenant", schema, "subject", stored)).ShouldBeTrue();
        JsonNode.DeepEquals(input, await manager.ReleaseStrict("store", "tenant", schema, "subject", stored)).ShouldBeTrue();
        JsonNode.DeepEquals(input, await manager.TryRelease("store", "tenant", schema, "subject", stored)).ShouldBeTrue();
        var prepared = await manager.TryPrepareForComparison(schema, stored, document =>
        {
            foreach (var member in Members(document, array))
            {
                member.Value!.GetValue<string>().StartsWith(VerificationMarkers.Prefix, StringComparison.Ordinal).ShouldBeTrue();
            }

            return document;
        });
        prepared.ShouldNotBeNull();
        JsonNode.DeepEquals(input, await manager.TryRelease("store", "tenant", schema, "subject", prepared!)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, "#/$defs/personal")]
    [InlineData(true, "#/$defs/personal")]
    [InlineData(false, "personal")]
    [InlineData(true, "personal")]
    public async Task should_release_previously_stored_member_ciphertext(bool array, string reference)
    {
        var (manager, handler, _) = CreateManager();
        var (schema, input) = Container(array, reference);
        var stored = (JsonObject)input.DeepClone();
        var members = Members(stored, array);
        foreach (var member in members.ToArray())
        {
            // Build the persisted member-level representation independently of the manager's traversal.
            members[member.Key] = await handler.Apply("store", "tenant", "subject", member.Value!);
        }

        JsonNode.DeepEquals(input, await manager.Release("store", "tenant", schema, "subject", stored)).ShouldBeTrue();
        JsonNode.DeepEquals(input, await manager.ReleaseStrict("store", "tenant", schema, "subject", stored)).ShouldBeTrue();
        JsonNode.DeepEquals(input, await manager.TryRelease("store", "tenant", schema, "subject", stored)).ShouldBeTrue();
    }

    static JsonObject Members(JsonObject document, bool array) =>
        (array ? document["value"]![0] : document["value"])!.AsObject();

    static (JsonSchema Schema, JsonObject Input) Container(bool array, string reference)
    {
        var root = JsonNode.Parse("""
            {
              "type":"object",
              "properties":{"value":{"$ref":"#/$defs/personal"}},
              "$defs":{
                "person":{
                  "type":"object",
                  "compliance":[{"metadataType":"PII","details":""}],
                  "properties":{"name":{"type":"string"},"email":{"type":"string"}}
                }
              }
            }
            """)!.AsObject();
        var definitions = root["$defs"]!.AsObject();
        definitions["personal"] = array
            ? JsonNode.Parse("""
                {"$id":"personal","type":"array","compliance":[{"metadataType":"PII","details":""}],"items":{"$ref":"#/$defs/person"}}
                """)
            : definitions["person"]!.DeepClone();
        definitions["personal"]!["$id"] = "personal";
        root["properties"]!["value"]!["$ref"] = reference;
        var person = JsonNode.Parse("""{"name":"Ada","email":"ada@example.com"}""")!;
        return (new JsonSchema(root), new JsonObject { ["value"] = array ? new JsonArray(person) : person });
    }

    static (JsonSchemaMetadataManager Manager, PIICompliancePropertyValueHandler Handler, Encryption Encryption) CreateManager()
    {
        var encryption = new Encryption();
        var keys = new InMemoryEncryptionKeyStorage();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        return (manager, handler, encryption);
    }
}
