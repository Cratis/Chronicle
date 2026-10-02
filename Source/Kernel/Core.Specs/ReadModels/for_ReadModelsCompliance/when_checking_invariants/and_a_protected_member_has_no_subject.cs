// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_a_protected_member_has_no_subject
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task should_not_bypass_protection_through_an_empty_subject(bool erased, bool partialSubjects, bool alternateCasing)
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},"other":{"type":"string"}}}""");
        var input = JsonNode.Parse("""{"value":"personal-value","other":"public-value"}""")!.AsObject();
        if (alternateCasing)
        {
            input["Value"] = input["value"]!.DeepClone();
            input.Remove("value");
        }
        if (partialSubjects)
        {
            input["__subjects"] = new JsonObject { ["other"] = "subject" };
        }
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var state = converter.ToExpandoObject(input, new JsonSchema());
        var keys = new InMemoryEncryptionKeyStorage();
        if (erased) await keys.RecordErasureFor("store", "Default", "subject");
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(manager, converter);
        var json = await compliance.ReleaseJson("store", "Default", schema, input);
        var expando = await compliance.Release("store", "Default", schema, state);
        json[alternateCasing ? "Value" : "value"]!.GetValue<string>().ShouldEqual(string.Empty);
        given.compliance_matrix.At(expando, "value")!.GetValue<string>().ShouldEqual(string.Empty);
        if (partialSubjects)
        {
            var applyError = await Catch.Exception(() => compliance.Apply("store", "Default", schema, string.Empty, state));
            applyError.ShouldBeOfExactType<UnresolvedSchemaProtection>();
        }
        (await keys.HasFor("store", "Default", "subject")).ShouldBeFalse();
    }
}
