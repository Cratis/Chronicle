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

public class and_unprotected_dynamic_members_accompany_pii
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_protect_the_personal_member_without_refusing_unrelated_dictionary_values(bool erased)
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
                "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "headers":{"type":"object","additionalProperties":{"type":"string"}},
                "count":{"type":"integer","if":{"minimum":0},"then":{"maximum":10}}
            }}
            """);
        var keys = new InMemoryEncryptionKeyStorage();
        if (erased) await keys.RecordErasureFor("store", "Default", "subject");
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
        var original = given.compliance_matrix.State(("name", "personal-value"), ("headers", new Dictionary<string, string> { ["X-Correlation-Id"] = "public-value" }), ("count", 3));
        var stored = await compliance.Apply("store", "Default", schema, "subject", original);
        var released = await compliance.Release("store", "Default", schema, stored);
        var document = JsonSerializer.SerializeToNode(stored)!.AsObject();
        Assert.NotEqual("personal-value", document["name"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(original)!["headers"], document["headers"]));
        Assert.Equal("public-value", JsonSerializer.SerializeToNode(released)!["headers"]!["X-Correlation-Id"]!.GetValue<string>());
        Assert.Equal(erased ? string.Empty : "personal-value", JsonSerializer.SerializeToNode(released)!["name"]!.GetValue<string>());
        Assert.Equal(3, document["count"]!.GetValue<int>());
        (await keys.HasFor("store", "Default", "subject")).ShouldEqual(!erased);
    }
}
