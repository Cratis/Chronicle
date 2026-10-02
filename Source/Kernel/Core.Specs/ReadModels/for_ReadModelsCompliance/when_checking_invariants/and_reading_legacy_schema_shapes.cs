// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_reading_legacy_schema_shapes
{
    public static TheoryData<string, string, string, bool> Cells
    {
        get
        {
            var cells = new TheoryData<string, string, string, bool>();
            var combinations = from union in new[] { "anyOf", "oneOf" }
                               from shape in new[] { "inline", "reference", "allOf", "allOf_reference" }
                               from marker in new[] { "none", "sibling", "member", "container" }
                               from erased in new[] { false, true }
                               select (union, shape, marker, erased);
            foreach (var (union, shape, marker, erased) in combinations) cells.Add(union, shape, marker, erased);
            return cells;
        }
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public async Task should_accept_main_compatible_values_and_read_main_ciphertext(string union, string shape, string marker, bool erased)
    {
        var target = new JsonObject { [union] = JsonNode.Parse("""[{"type":"object","properties":{"name":{"type":"string"}}},{"type":"object","properties":{"other":{"type":"string"}}}]""") };
        var member = shape switch
        {
            "reference" => new JsonObject { ["$ref"] = "#/$defs/value" },
            "allOf" => new JsonObject { ["allOf"] = new JsonArray(target.DeepClone()) },
            "allOf_reference" => new JsonObject { ["allOf"] = new JsonArray(new JsonObject { ["$ref"] = "#/$defs/value" }) },
            _ => (JsonObject)target.DeepClone()
        };
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject { ["value"] = member },
            ["$defs"] = new JsonObject { ["value"] = target }
        };
        var whole = marker == "member" || marker == "container";
        if (marker == "member") member["compliance"] = Marker();
        if (marker == "container") root["compliance"] = Marker();
        if (whole) member["type"] = "object";
        var input = JsonNode.Parse(whole ? """{"value":{"Name":"Alice","nothing":null,"child":{"Name":"Child"}}}""" : """{"value":{"name":"Alice"}}""")!.AsObject();
        if (marker != "none")
        {
            root["properties"]!["guard"] = new JsonObject { ["type"] = "string", ["compliance"] = Marker() };
            input["guard"] = "private";
        }
        var schema = await JsonSchema.FromJsonAsync(root.ToJsonString());
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(manager, converter);

        // Main blob-encrypts these directly marked members without evaluating their union branches.
        var legacy = (JsonObject)input.DeepClone();
        if (whole) legacy["value"] = await handler.Apply("store", "Default", "subject", input["value"]!);
        if (marker != "none") legacy["guard"] = await handler.Apply("store", "Default", "subject", input["guard"]!);
        if (erased)
        {
            await keys.RecordErasureFor("store", "Default", "subject");
            await keys.DeleteFor("store", "Default", "subject");
        }

        Check(await manager.Release("store", "Default", schema, "subject", legacy));
        legacy["__subject"] = "subject";
        Check(await compliance.ReleaseJson("store", "Default", schema, legacy));
        var legacyState = converter.ToExpandoObject(legacy, new JsonSchema());
        Check(JsonSerializer.SerializeToNode(await compliance.Release("store", "Default", schema, legacyState))!.AsObject());

        var appendError = await Catch.Exception(() => manager.Apply("store", "Default", schema, "subject", input));
        if (erased && marker != "none") Assert.IsType<SchemaMetadataActionFailed>(appendError);
        else Assert.Null(appendError);
        var state = converter.ToExpandoObject(input, new JsonSchema());
        var applied = await compliance.Apply("store", "Default", schema, "subject", state);
        CheckStored(applied);
        var reduced = await and_preserving_union_compatibility.Reduce(compliance, schema, state);
        CheckStored(reduced);
        Check(JsonSerializer.SerializeToNode(await compliance.Release("store", "Default", schema, reduced))!.AsObject());
        var released = await compliance.Release("store", "Default", schema, applied);
        Check(JsonSerializer.SerializeToNode(released)!.AsObject());
        var replayed = await compliance.Apply("store", "Default", schema, "subject", released);
        Check(JsonSerializer.SerializeToNode(await compliance.Release("store", "Default", schema, replayed))!.AsObject());
        if (erased) (await keys.HasFor("store", "Default", "subject")).ShouldBeFalse();

        void CheckStored(ExpandoObject stored)
        {
            if (!whole) return;
            var text = given.compliance_matrix.At(stored, "value")!.GetValue<string>();
            Assert.True(erased ? text.Length == 0 : ProtectedValueCodec.TryDecodeCipherText(encryption, text, out _), "I1: protected values cannot be stored in cleartext");
        }

        void Check(JsonObject actual)
        {
            var expected = erased && whole ? new JsonObject() : input["value"];
            Assert.True(JsonNode.DeepEquals(expected, actual["value"]), $"I2/I3: expected {expected}, got {actual["value"]}");
            if (marker != "none") actual["guard"]!.GetValue<string>().ShouldEqual(erased ? string.Empty : "private");
            if (!erased && whole) Assert.False(actual["value"]!.AsObject().ContainsKey("name"), "A schema spelling must not duplicate an existing member");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_read_members_encrypted_by_main_inside_a_marked_array_item(bool erased)
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"items":{"type":"array","items":{"type":"object","compliance":[{"metadataType":"PII","details":""}],"properties":{"name":{"type":"string"},"count":{"type":"integer"}}}}}}""");
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        var legacy = new JsonObject
        {
            ["items"] = new JsonArray(new JsonObject
            {
                ["name"] = await handler.Apply("store", "Default", "subject", JsonValue.Create("Alice")),
                ["count"] = await handler.Apply("store", "Default", "subject", JsonValue.Create(42))
            })
        };
        if (erased)
        {
            await keys.RecordErasureFor("store", "Default", "subject");
            await keys.DeleteFor("store", "Default", "subject");
        }
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        var released = await manager.Release("store", "Default", schema, "subject", legacy);
        released["items"]![0]!["name"]!.GetValue<string>().ShouldEqual(erased ? string.Empty : "Alice");
        released["items"]![0]!["count"]!.GetValue<int>().ShouldEqual(erased ? 0 : 42);
    }

    static JsonArray Marker() => new(new JsonObject { ["metadataType"] = "PII", ["details"] = string.Empty });
}
