// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

using ResourceMatrix = Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants.given.compatibility_matrix;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_resolving_resources;

public class and_protecting_referenced_values
{
    [Theory]
    [InlineData("resource_relative")]
    [InlineData("anchor_scoped")]
    [InlineData("plain_name_collision")]
    [InlineData("pointer")]
    public async Task should_encrypt_and_round_trip_the_referenced_value(string shape)
    {
        var (manager, _, encryption) = CreateManager();
        var (root, input) = ResourceMatrix.Resource(shape);
        var schema = new JsonSchema(root);
        var property = input.GetAt(0).Key;
        var stored = await manager.Apply("store", "tenant", schema, "subject", input);
        ProtectedValueCodec.TryDecodeCipherText(encryption, stored[property]!.GetValue<string>(), out _).ShouldBeTrue();
        JsonNode.DeepEquals(input, stored).ShouldBeFalse();
        var released = await manager.Release("store", "tenant", schema, "subject", stored);
        JsonNode.DeepEquals(input, released).ShouldBeTrue();
        var strict = await manager.ReleaseStrict("store", "tenant", schema, "subject", stored);
        JsonNode.DeepEquals(input, strict).ShouldBeTrue();
        var comparison = await manager.TryRelease("store", "tenant", schema, "subject", stored);
        JsonNode.DeepEquals(input, comparison).ShouldBeTrue();
    }

    [Theory]
    [InlineData("resource_relative")]
    [InlineData("anchor_scoped")]
    [InlineData("plain_name_collision")]
    [InlineData("pointer")]
    public async Task should_not_store_or_release_personal_values_after_erasure(string shape)
    {
        var (manager, keys, _) = CreateManager();
        var (root, input) = ResourceMatrix.Resource(shape);
        var schema = new JsonSchema(root);
        var property = input.GetAt(0).Key;
        var stored = await manager.Apply("store", "tenant", schema, "subject", input);
        await keys.RecordErasureFor("store", "tenant", "subject");
        await keys.DeleteFor("store", "tenant", "subject");
        var released = await manager.Release("store", "tenant", schema, "subject", stored);
        released[property]!.GetValue<string>().ShouldBeEmpty();
        var strict = await manager.ReleaseStrict("store", "tenant", schema, "subject", stored);
        strict[property]!.GetValue<string>().ShouldBeEmpty();
        var failure = await Catch.Exception(() => manager.Apply("store", "tenant", schema, "subject", input));
        failure.ShouldBeOfExactType<SchemaMetadataActionFailed>();
        (await keys.TryGetFor("store", "tenant", "subject")).ShouldBeNull();
    }

    [Theory]
    [InlineData("resource_relative", false)]
    [InlineData("resource_relative", true)]
    [InlineData("anchor_scoped", false)]
    [InlineData("anchor_scoped", true)]
    [InlineData("plain_name_collision", false)]
    [InlineData("plain_name_collision", true)]
    [InlineData("pointer", false)]
    [InlineData("pointer", true)]
    public async Task should_keep_legacy_plaintext_readable_without_retroactive_rejection(string shape, bool erased)
    {
        var (manager, keys, _) = CreateManager();
        var (root, input) = ResourceMatrix.Resource(shape);
        var schema = new JsonSchema(root);
        if (erased) await keys.RecordErasureFor("store", "tenant", "subject");
        var released = await manager.Release("store", "tenant", schema, "subject", input);
        var strict = await manager.ReleaseStrict("store", "tenant", schema, "subject", input);
        var comparison = await manager.TryRelease("store", "tenant", schema, "subject", input);
        JsonNode.DeepEquals(input, released).ShouldBeTrue();
        JsonNode.DeepEquals(input, strict).ShouldBeTrue();

        // Comparison release requires ciphertext; legacy plaintext stays readable through the release APIs,
        // but is unavailable to this fail-closed probe. Comparison preparation preserves it separately.
        comparison.ShouldBeNull();
    }

    [Theory]
    [InlineData("resource_relative")]
    [InlineData("anchor_scoped")]
    [InlineData("plain_name_collision")]
    [InlineData("pointer")]
    public async Task should_compare_old_plaintext_and_new_ciphertext_without_exposing_conversion_values(string shape)
    {
        var (manager, _, _) = CreateManager();
        var (root, input) = ResourceMatrix.Resource(shape);
        var schema = new JsonSchema(root);
        var stored = await manager.Apply("store", "tenant", schema, "subject", input);
        var legacy = await manager.TryPrepareForComparison(schema, input, document =>
        {
            JsonNode.DeepEquals(document, input).ShouldBeFalse();
            return document;
        });
        var current = await manager.TryPrepareForComparison(schema, stored, document => document);
        var released = await manager.TryRelease("store", "tenant", schema, "subject", current!);
        JsonNode.DeepEquals(input, legacy).ShouldBeTrue();
        JsonNode.DeepEquals(legacy, released).ShouldBeTrue();
    }

    [Theory]
    [InlineData("resource_relative")]
    [InlineData("anchor_scoped")]
    [InlineData("plain_name_collision")]
    [InlineData("pointer")]
    public async Task should_report_the_same_boundary_for_encryption_and_location(string shape)
    {
        var (manager, _, encryption) = CreateManager();
        var (root, input) = ResourceMatrix.Resource(shape);
        var schema = new JsonSchema(root);
        var located = await manager.LocateProtectedValues(schema, "subject", input);
        var applied = await manager.ApplyReportingKeys("store", "tenant", schema, "subject", input);
        located.Count.ShouldEqual(1);
        applied.Uses.Count.ShouldEqual(1);
        located[0].Path.ShouldEqual(input.GetAt(0).Key);
        applied.Uses[0].Path.ShouldEqual(located[0].Path);
        ProtectedValueCodec.TryDecodeCipherText(encryption, applied.Value[input.GetAt(0).Key]!.GetValue<string>(), out _).ShouldBeTrue();
    }

    [Fact]
    public async Task should_not_add_an_append_refusal_for_an_ambiguous_referenced_definition()
    {
        var (manager, _, _) = CreateManager();
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"value":{"$ref":"personal"}},"$defs":{"member":{"$id":"personal","type":["string","integer"],"compliance":[{"metadataType":"PII","details":""}]}}}
            """);
        var input = JsonNode.Parse("""{"value":"legacy"}""")!.AsObject();
        var result = await manager.Apply("store", "tenant", schema, "subject", input);
        JsonNode.DeepEquals(input, result).ShouldBeTrue();
    }

    [Fact]
    public async Task should_not_apply_a_duplicate_marker_twice()
    {
        var (manager, _, _) = CreateManager();
        var (root, input) = ResourceMatrix.Resource("resource_relative");
        root["properties"]!["value"]!["compliance"] = root["$defs"]!["member"]!["compliance"]!.DeepClone();
        var schema = new JsonSchema(root);
        var stored = await manager.Apply("store", "tenant", schema, "subject", input);
        var released = await manager.Release("store", "tenant", schema, "subject", stored);
        JsonNode.DeepEquals(input, released).ShouldBeTrue();
    }

    static (JsonSchemaMetadataManager Manager, InMemoryEncryptionKeyStorage Keys, Encryption Encryption) CreateManager()
    {
        var encryption = new Encryption();
        var keys = new InMemoryEncryptionKeyStorage();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        return (manager, keys, encryption);
    }
}
