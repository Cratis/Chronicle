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

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_resolving_resources;

public class and_protecting_referenced_values
{
    [Theory]
    [InlineData("resource_relative", false)]
    [InlineData("resource_relative", true)]
    [InlineData("anchor_scoped", false)]
    [InlineData("anchor_scoped", true)]
    [InlineData("plain_name_collision", false)]
    [InlineData("plain_name_collision", true)]
    public async Task should_protect_apply_and_both_release_boundaries(string shape, bool erased)
    {
        var encryption = new Encryption();
        var keys = new InMemoryEncryptionKeyStorage();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var compliance = new ReadModelsCompliance(manager, converter);
        var (root, input) = when_checking_invariants.given.compatibility_matrix.Resource(shape);
        var property = input.GetAt(0).Key;
        var schema = new JsonSchema(root);
        var state = converter.ToExpandoObject(input, new JsonSchema());
        ((IDictionary<string, object?>)state)["__subject"] = "subject";
        var stored = await compliance.Apply("store", "tenant", schema, "subject", state);
        var storedJson = JsonSerializer.SerializeToNode(stored)!.AsObject();
        ProtectedValueCodec.TryDecodeCipherText(encryption, storedJson[property]!.GetValue<string>(), out _).ShouldBeTrue();
        if (erased)
        {
            await keys.RecordErasureFor("store", "tenant", "subject");
            await keys.DeleteFor("store", "tenant", "subject");
        }
        var released = await compliance.Release("store", "tenant", schema, stored);
        var releasedJson = await compliance.ReleaseJson("store", "tenant", schema, storedJson);
        var expected = erased ? JsonValue.Create(string.Empty) : input[property];
        JsonNode.DeepEquals(JsonSerializer.SerializeToNode(released)![property], expected).ShouldBeTrue();
        JsonNode.DeepEquals(releasedJson[property], expected).ShouldBeTrue();
        var reapplied = await compliance.Apply("store", "tenant", schema, "subject", state);
        var reappliedJson = JsonSerializer.SerializeToNode(reapplied)!;
        if (erased)
        {
            reappliedJson[property]!.GetValue<string>().ShouldBeEmpty();
            (await keys.TryGetFor("store", "tenant", "subject")).ShouldBeNull();
        }
        else
        {
            ProtectedValueCodec.TryDecodeCipherText(encryption, reappliedJson[property]!.GetValue<string>(), out _).ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("resource_relative")]
    [InlineData("anchor_scoped")]
    [InlineData("plain_name_collision")]
    public async Task should_preserve_the_pii_free_control(string shape)
    {
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var compliance = new ReadModelsCompliance(Substitute.For<IJsonSchemaMetadataManager>(), converter);
        var (root, input) = when_checking_invariants.given.compatibility_matrix.Resource(shape, pii: false);
        var state = converter.ToExpandoObject(input, new JsonSchema());
        var schema = new JsonSchema(root);
        var stored = await compliance.Apply("store", "tenant", schema, "subject", state);
        var released = await compliance.Release("store", "tenant", schema, stored);
        JsonNode.DeepEquals(JsonSerializer.SerializeToNode(released)![input.GetAt(0).Key], input.GetAt(0).Value).ShouldBeTrue();
    }
}
