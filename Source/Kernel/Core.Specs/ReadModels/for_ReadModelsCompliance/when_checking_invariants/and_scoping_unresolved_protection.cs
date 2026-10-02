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

public class and_scoping_unresolved_protection
{
    public static TheoryData<string, string, bool, bool> Cells
    {
        get
        {
            var cells = new TheoryData<string, string, bool, bool>();
            string[] shapes = ["dictionary", "referenced_dictionary", "oneOf", "anyOf", "if", "then", "else", "not", "patternProperties", "dependentSchemas", "dependencies", "tuple", "prefixItems", "contains", "conflict", "null_branch", "dynamic_reference", "nested_dynamic_reference", "anchored_dictionary", "anchored_anyOf", "anchored_oneOf", "legacy_anchored_dictionary"];
            var combinations = from shape in shapes
                               from category in new[] { "compliance", "security" }
                               from protectedValue in new[] { false, true }
                               from erased in new[] { false, true }
                               select (shape, category, protectedValue, erased);
            foreach (var (shape, category, protectedValue, erased) in combinations) cells.Add(shape, category, protectedValue, erased);
            return cells;
        }
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public async Task should_refuse_only_unresolved_protection_and_leave_plain_schemas_compatible(string shape, string category, bool protectedValue, bool erased)
    {
        var marker = protectedValue ? $",\"{category}\":[{{\"metadataType\":\"PII\",\"details\":\"\"}}]" : string.Empty;
        var leaf = $$"""{"type":"string"{{marker}}} """;
        var member = $$"""{"type":"object","properties":{"value":{{leaf}} } }""";
        const string Plain = """{"type":"object","properties":{"value":{"type":"string"}}}""";
        var schemaText = shape switch
        {
            "dictionary" => $$"""{"type":"object","additionalProperties":{{leaf}}} """,
            "referenced_dictionary" => $$"""{"type":"object","additionalProperties":{"$ref":"#/$defs/alias"},"$defs":{"alias":{"$ref":"#/$defs/leaf"},"leaf":{{leaf}} } }""",
            "anchored_dictionary" or "legacy_anchored_dictionary" => $$"""{"type":"object","additionalProperties":{"$ref":"#personal"},"{{(shape == "legacy_anchored_dictionary" ? "definitions" : "$defs")}}":{"member":{"$anchor":"personal","type":"string"{{marker}} } } }""",
            "anchored_anyOf" or "anchored_oneOf" => $$"""{"{{(shape == "anchored_anyOf" ? "anyOf" : "oneOf")}}":[{{Plain}},{"type":"object","properties":{"value":{"$ref":"#personal"} } }],"$defs":{"member":{"$anchor":"personal","type":"string"{{marker}} } } }""",
            "oneOf" or "anyOf" => $$"""{"{{shape}}":[{{Plain}},{{member}}]}""",
            "if" or "then" or "else" or "not" => $$"""{"type":"object","properties":{"value":{"type":"string"} },"{{shape}}":{{member}} }""",
            "patternProperties" => $$"""{"type":"object","patternProperties":{"^value$":{{leaf}} } }""",
            "dependentSchemas" or "dependencies" => $$"""{"type":"object","{{shape}}":{"value":{{member}} } }""",
            "tuple" => $$"""{"type":"object","properties":{"value":{"type":"array","items":[{{leaf}}]} } }""",
            "prefixItems" or "contains" => $$"""{"type":"object","properties":{"value":{"type":"array","{{shape}}":{{(shape == "prefixItems" ? $"[{leaf}]" : leaf)}} } } }""",
            "conflict" => $$"""{"properties":{"value":{{leaf}} },"allOf":[{"type":"object"},{"type":"string"}]}""",
            "null_branch" => $$"""{"type":"object","properties":{"value":{"anyOf":[{"type":"string"},{"type":"null"{{marker}} }]} } }""",
            "dynamic_reference" => $$"""{"type":"object","properties":{"value":{"type":"string","$dynamicRef":"#personal"} },"$defs":{"personal":{"$dynamicAnchor":"personal","type":"string"{{marker}} } } }""",
            _ => $$"""{"type":"object","properties":{"value":{"type":"string","$dynamicRef":"#personal"} },"$defs":{"unused":{"$defs":{"personal":{"$dynamicAnchor":"personal","type":"string"{{marker}} } } } } }"""
        };
        var schema = await JsonSchema.FromJsonAsync(schemaText);
        var isArray = shape == "tuple" || shape == "prefixItems" || shape == "contains";
        var input = JsonNode.Parse(isArray ? """{"value":["personal-value"]}""" : """{"value":"personal-value"}""")!.AsObject();
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        if (erased) await keys.RecordErasureFor("store", "Default", "subject");
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(manager, converter);
        var state = given.compliance_matrix.State(("value", isArray ? new[] { "personal-value" } : "personal-value"), ("__subject", "subject"));
        Func<Task<object>>[] actions =
        [
            async () => await manager.Apply("store", "Default", schema, "subject", input),
            async () => await manager.ApplyToReadModel("store", "Default", schema, "subject", input),
            async () => await manager.Release("store", "Default", schema, "subject", input),
            async () => await compliance.Apply("store", "Default", schema, "subject", state),
            async () => await compliance.Release("store", "Default", schema, state),
            async () => await compliance.ReleaseJson("store", "Default", schema, input)
        ];
        schema.HasSchemaMetadata().ShouldEqual(protectedValue);
        foreach (var action in actions)
        {
            object? result = null;
            var error = await Catch.Exception(async () => result = await action());
            if (protectedValue)
            {
                Assert.IsType<UnresolvedSchemaProtection>(error);
                Assert.Null(result);
            }
            else
            {
                Assert.Null(error);
                Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(result), result is JsonObject ? input : JsonSerializer.SerializeToNode(state)), "INVARIANTS: I3, I4 — a PII-free compliance pass must not change the document");
            }
        }
        if (!protectedValue)
        {
            var converted = converter.ToExpandoObject(input, schema);
            Assert.True(JsonNode.DeepEquals(converter.ToJsonObject(converted, schema), input), "INVARIANTS: I3 — plain conversion retains main's supported values");
        }
        (await keys.HasFor("store", "Default", "subject")).ShouldBeFalse();
        Assert.Contains("personal-value", input.ToJsonString(), StringComparison.Ordinal);
    }
}
