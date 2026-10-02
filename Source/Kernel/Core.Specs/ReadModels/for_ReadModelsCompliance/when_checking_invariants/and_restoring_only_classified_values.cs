// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_restoring_only_classified_values
{
    public static TheoryData<string, bool> Cells
    {
        get
        {
            var cells = new TheoryData<string, bool>();
            foreach (var kind in new[] { "unprotected", "member", "reference", "composition", "container", "unresolved", "duplicate" })
            {
                cells.Add(kind, false);
                cells.Add(kind, true);
            }
            return cells;
        }
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public async Task should_not_restore_an_original_without_a_complete_unprotected_classification(string kind, bool erased)
    {
        const string Marker = "\"compliance\":[{\"metadataType\":\"PII\",\"details\":\"\"}]";
        var property = kind switch
        {
            "member" => $$"""{"allOf":[{"type":"string"}],{{Marker}}}""",
            "reference" => """{"allOf":[{"$ref":"#/$defs/personal"}]}""",
            "composition" => $$"""{"allOf":[{"type":"string"},{ {{Marker}} }]}""",
            "unresolved" => """{"$ref":"#/$defs/missing"}""",
            "duplicate" => """{"allOf":[{"type":"integer"}]}""",
            _ => """{"allOf":[{"type":"string"}]}"""
        };
        var composition = kind switch
        {
            "container" => $$""", "allOf":[{ {{Marker}} }]""",
            "duplicate" => $$""", "allOf":[{"properties":{"value":{"allOf":[{"type":"integer"}],{{Marker}} } } }]""",
            _ => string.Empty
        };
        var schema = await JsonSchema.FromJsonAsync($$"""
            {"type":"object","$defs":{"personal":{"type":"string",{{Marker}} } },
            "properties":{"value":{{property}},"guard":{"type":"string",{{Marker}} } } {{composition}} }
            """);
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
        if (erased) await keys.RecordErasureFor("store", "Default", "subject");
        object original = kind == "duplicate" ? 123 : "original";
        ExpandoObject? stored = null;
        var error = await Catch.Exception(async () => stored = await compliance.Apply("store", "Default", schema, "subject", given.compliance_matrix.State(("value", original), ("guard", "guard"))));
        if (kind == "unresolved")
        {
            // Main also rejects this unresolved schema. A failed operation is closed, not a restored value.
            error.ShouldBeOfExactType<NullReferenceException>();
            stored.ShouldBeNull();
            return;
        }
        Assert.True(error is null, $"INVARIANTS: freeze\n{error}");
        var restored = ((IDictionary<string, object?>)stored!).TryGetValue("value", out var value) && Equals(value, original);
        Assert.True(restored == (kind == "unprotected"), $"INVARIANTS: I6\n{kind}: original restored = {restored}");
    }
}
