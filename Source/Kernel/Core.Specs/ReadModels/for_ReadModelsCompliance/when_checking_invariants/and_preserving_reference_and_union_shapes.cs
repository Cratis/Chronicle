// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

public class and_preserving_reference_and_union_shapes
{
    static readonly string[] _shapes = ["definitions_scalar", "defs_scalar", "definitions_object", "defs_object", "properties_scalar", "plain_name", "self", "mutual", "ancestor_anchor", "recursive_anyOf", "recursive_oneOf", "items_anyOf", "items_oneOf", "whole_anyOf", "whole_oneOf", "whole_allOf_anyOf", "whole_allOf_oneOf", "whole_root_anyOf", "whole_root_oneOf", "unprotected_anyOf", "unprotected_oneOf"];

    public static TheoryData<string, bool, bool> Cells
    {
        get
        {
            var cells = new TheoryData<string, bool, bool>();
            var combinations = from shape in _shapes
                               from pii in new[] { false, true }
                               from erased in new[] { false, true }
                               select (shape, pii, erased);
            foreach (var (shape, pii, erased) in combinations) cells.Add(shape, pii, erased);
            return cells;
        }
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public async Task should_preserve_protection_erasure_and_live_values(string shape, bool pii, bool erased)
    {
        var (schema, input, paths) = Create(shape, pii);

        // Same reference text and schema identity fields, different instances/generations and protection.
        // Warm the opposite answer first, then reuse each cache after operating on the other schema.
        var (other, _, _) = Create(shape, !pii);
        other.HasSchemaMetadata().ShouldEqual(!pii);
        other.IsUnprotectedSchemaValue().ShouldEqual(pii);
        other.EnsureProtectionCanBeResolved();
        _ = other.ResolveComposition();
        schema.HasSchemaMetadata().ShouldEqual(pii);
        schema.IsUnprotectedSchemaValue().ShouldEqual(!pii);
        schema.EnsureProtectionCanBeResolved();
        foreach (var property in schema.GetFlattenedProperties()) _ = property.GetFlattenedProperties().ToArray();

        var converter = new ExpandoObjectConverter(new TypeFormats());
        var state = converter.ToExpandoObject(input, schema);
        Assert.True(JsonNode.DeepEquals(input, converter.ToJsonObject(state, schema)), "I3: conversion lost a live value");
        var encryption = new Encryption();
        var keys = new InMemoryEncryptionKeyStorage();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(manager, converter);
        if (pii && shape.StartsWith("unprotected_", StringComparison.Ordinal))
        {
            var alternateCase = (JsonObject)input.DeepClone();
            alternateCase.Remove("guard");
            alternateCase["Guard"] = "private";
            await Assert.ThrowsAsync<SchemaPropertyNotFoundInSchema>(() => manager.Apply("store", "Default", schema, "subject", alternateCase));
            await Assert.ThrowsAsync<SchemaPropertyNotFoundInSchema>(() => manager.Release("store", "Default", schema, "subject", alternateCase));
        }
        var appended = await manager.Apply("store", "Default", schema, "subject", input);
        CheckStored(appended, false);
        CheckReleased(await manager.Release("store", "Default", schema, "subject", appended), false);
        var stored = await compliance.Apply("store", "Default", schema, "subject", state);
        CheckStored(stored, false);
        CheckReleased(await compliance.Release("store", "Default", schema, stored), false);
        CheckReleased(await compliance.ReleaseJson("store", "Default", schema, JsonSerializer.SerializeToNode(stored)!.AsObject()), false);

        if (erased)
        {
            await keys.RecordErasureFor("store", "Default", "subject");
            await keys.DeleteFor("store", "Default", "subject");
            CheckReleased(await manager.Release("store", "Default", schema, "subject", appended), true);
            CheckReleased(await compliance.Release("store", "Default", schema, stored), true);
            CheckReleased(await compliance.ReleaseJson("store", "Default", schema, JsonSerializer.SerializeToNode(stored)!.AsObject()), true);
            var appendError = await Catch.Exception(() => manager.Apply("store", "Default", schema, "subject", input));
            if (pii) Assert.IsType<SchemaMetadataActionFailed>(appendError);
            else Assert.Null(appendError);
            var updated = await compliance.Apply("store", "Default", schema, "subject", converter.ToExpandoObject(input, schema));
            CheckStored(updated, true);
            CheckReleased(await compliance.Release("store", "Default", schema, updated), true);
            var plaintext = (JsonObject)input.DeepClone();
            plaintext["__subject"] = "subject";
            CheckReleased(await compliance.ReleaseJson("store", "Default", schema, plaintext), true);
            (await keys.HasFor("store", "Default", "subject")).ShouldBeFalse();
        }
        other.IsUnprotectedSchemaValue().ShouldEqual(pii);
        schema.IsUnprotectedSchemaValue().ShouldEqual(!pii);

        void CheckStored(object result, bool isErased)
        {
            if (!pii)
            {
                CheckReleased(result, false);
                return;
            }
            foreach (var path in paths)
            {
                var value = given.compliance_matrix.At(result, path);
                var text = value?.GetValue<string>();
                Assert.True(isErased ? text?.Length == 0 : ProtectedValueCodec.TryDecodeCipherText(encryption, text!, out _), $"I1: {shape}/{path} was not protected");
            }
        }
        void CheckReleased(object result, bool isErased)
        {
            if (shape.StartsWith("unprotected_", StringComparison.Ordinal))
            {
                Assert.True(JsonNode.DeepEquals(input["value"], given.compliance_matrix.At(result, "value")), "I3: unprotected union member changed");
                Assert.True(JsonNode.DeepEquals(input["extra"], given.compliance_matrix.At(result, "extra")), "I3: undeclared union member changed");
            }
            foreach (var path in paths)
            {
                var expected = given.compliance_matrix.At(input, path);
                if (isErased && pii) expected = expected is JsonObject ? new JsonObject() : JsonValue.Create(string.Empty);
                var actual = given.compliance_matrix.At(result, path);
                Assert.True(JsonNode.DeepEquals(expected, actual), $"{(isErased && pii ? "I2" : "I3")}: {shape}/{path}: expected {expected}, actual {actual}");
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_isolate_composition_caches_by_protection_context(bool protectedFirst)
    {
        var schema = JsonSchema.FromJson("""{"anyOf":[{"type":"object","properties":{"name":{"type":"string"}}},{"type":"null"}]}""");
        _ = schema.ResolveComposition(protectedFirst);
        schema.ResolveComposition(true).Properties.Keys.ShouldContain("name");
        schema.ResolveComposition().Properties.ShouldBeEmpty();
        schema.IsUnprotectedSchemaValue().ShouldBeTrue();
    }

    [Theory]
    [InlineData("self", false)]
    [InlineData("self", true)]
    [InlineData("mutual", false)]
    [InlineData("mutual", true)]
    [InlineData("anchor", false)]
    [InlineData("anchor", true)]
    public void should_terminate_reference_cycles_without_losing_metadata(string cycle, bool pii)
    {
        var node = JsonNode.Parse("""{"$ref":"#/$defs/a","$defs":{"a":{"$ref":"#/$defs/a"},"b":{"$ref":"#/$defs/a"}}}""")!.AsObject();
        if (cycle == "mutual") node["$defs"]!["a"]!["$ref"] = "#/$defs/b";
        if (cycle == "anchor")
        {
            node["$anchor"] = "parent";
            node["$defs"]!["a"]!["$ref"] = "#parent";
        }
        if (pii) node["$defs"]!["a"]!["compliance"] = Marker();
        var schema = JsonSchema.FromJson(node.ToJsonString());
        schema.HasSchemaMetadata().ShouldEqual(pii);
        schema.IsUnprotectedSchemaValue().ShouldEqual(!pii);
        schema.GetFlattenedProperties().ShouldBeEmpty();
        Assert.Throws<UnresolvedSchemaProtection>(() => schema.ResolveComposition());
        if (pii) Assert.Throws<UnresolvedSchemaProtection>(schema.EnsureProtectionCanBeResolved);
        else schema.EnsureProtectionCanBeResolved();
    }

    static (JsonSchema Schema, JsonObject Input, string[] Paths) Create(string shape, bool pii)
    {
        var root = JsonNode.Parse("""{"title":"SameEvent","type":"object","properties":{},"$defs":{}}""")!.AsObject();
        var leaf = JsonNode.Parse("""{"type":"string"}""")!.AsObject();
        if (pii) leaf["compliance"] = Marker();
        var input = JsonNode.Parse("""{"value":"private"}""")!.AsObject();
        string[] paths = ["value"];
        if (shape == "definitions_scalar" || shape == "defs_scalar" || shape == "definitions_object" || shape == "defs_object" || shape == "properties_scalar" || shape == "plain_name")
        {
            var definition = leaf;
            if (shape.EndsWith("_object", StringComparison.Ordinal))
            {
                definition = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { ["name"] = leaf } };
                input["value"] = new JsonObject { ["name"] = "private" };
                paths = ["value.name"];
            }
            var key = shape.StartsWith("definitions", StringComparison.Ordinal) ? "definitions" : "$defs";
            if (shape == "properties_scalar") key = "properties";
            if (root[key] is null) root[key] = new JsonObject();
            root[key]!["Person"] = definition;
            var reference = $"#{key}/Person";
            if (shape == "plain_name")
            {
                root["Person"] = definition.DeepClone();
                reference = "#Person";
            }
            root["properties"]!["value"] = new JsonObject { ["$ref"] = reference };
        }
        else if (shape.StartsWith("unprotected_", StringComparison.Ordinal))
        {
            root[shape[12..]] = JsonNode.Parse("""[{"type":"object","properties":{"value":{"type":"string"}}},{"type":"null"}]""");
            if (pii) root["properties"]!["guard"] = leaf;
            input["guard"] = "private";
            input["extra"] = "retained";
            paths = ["guard"];
        }
        else if (shape.StartsWith("items_", StringComparison.Ordinal))
        {
            root["properties"]!["value"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = leaf,
                [shape[6..]] = JsonNode.Parse("""[{"type":"array"},{"type":"null"}]""")
            };
            input["value"] = new JsonArray("private", "second");
            paths = ["value.0", "value.1"];
        }
        else if (shape.StartsWith("whole_", StringComparison.Ordinal))
        {
            var union = shape.EndsWith("anyOf", StringComparison.Ordinal) ? "anyOf" : "oneOf";
            root["$defs"]!["Person"] = new JsonObject { [union] = JsonNode.Parse("""[{"type":"object","properties":{"name":{"type":"string"},"child":{"type":"object","properties":{"name":{"type":"string"}}}}},{"type":"null"}]""") };
            var member = new JsonObject { ["$ref"] = "#/$defs/Person" };
            if (shape.Contains("allOf", StringComparison.Ordinal)) member = new JsonObject { ["allOf"] = new JsonArray(member, new JsonObject()) };
            if (pii) (shape.Contains("root", StringComparison.Ordinal) ? root : member)["compliance"] = Marker();
            root["properties"]!["value"] = member;

            // Whole protection and reference-to-union conversion must retain the complete payload.
            input["value"] = JsonNode.Parse("""{"name":"Alice","Name":"Distinct","extra":"retained","nothing":null,"child":{"name":"Child","extra":"nested-retained"}}""");
            if (!pii) input["value"]!.AsObject().Remove("nothing");
        }
        else
        {
            var reference = shape == "ancestor_anchor" ? "#ancestor" : "#/$defs/Person";
            var next = shape == "mutual" ? "#/$defs/Alias" : reference;
            var person = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { ["name"] = leaf, ["next"] = new JsonObject { ["$ref"] = next } } };
            if (shape == "ancestor_anchor") person["$anchor"] = "ancestor";
            root["$defs"]!["Person"] = shape.StartsWith("recursive_", StringComparison.Ordinal)
                ? new JsonObject { [shape[10..]] = new JsonArray(person, new JsonObject { ["type"] = "null" }) }
                : person;
            root["$defs"]!["Alias"] = new JsonObject { ["$ref"] = reference };
            root["properties"]!["value"] = new JsonObject { ["$ref"] = reference };
            input["value"] = JsonNode.Parse("""{"name":"private","next":{"name":"second"}}""");
            paths = ["value.name", "value.next.name"];
        }
        return (JsonSchema.FromJson(root.ToJsonString()), input, paths);
    }

    static JsonArray Marker() => new(new JsonObject { ["metadataType"] = "PII", ["details"] = string.Empty });
}
