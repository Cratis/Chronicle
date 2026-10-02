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

public class and_refusing_unresolved_protection
{
    [Theory]
    [InlineData("reference", false)]
    [InlineData("reference", true)]
    [InlineData("union", false)]
    [InlineData("union", true)]
    [InlineData("conflict", false)]
    [InlineData("conflict", true)]
    [InlineData("dynamic", false)]
    [InlineData("dynamic", true)]
    [InlineData("cycle", false)]
    [InlineData("cycle", true)]
    [InlineData("tuple", false)]
    [InlineData("tuple", true)]
    [InlineData("malformed", false)]
    [InlineData("malformed", true)]
    public async Task should_refuse_a_write_instead_of_completing_with_unresolved_protection(string shape, bool erased)
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
        var compliance = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
        if (erased) await keys.RecordErasureFor("store", "Default", "subject");
        var input = given.compliance_matrix.State(("guard", "guard"), ("value", "personal-value"));
        ExpandoObject? stored = null;
        var error = await Catch.Exception(async () => stored = await compliance.Apply("store", "Default", schema, "subject", input));
        error.ShouldBeOfExactType<UnresolvedSchemaProtection>();
        stored.ShouldBeNull();
        (await keys.HasFor("store", "Default", "subject")).ShouldBeFalse();
        ((IDictionary<string, object?>)input)["value"].ShouldEqual("personal-value");
    }
}
