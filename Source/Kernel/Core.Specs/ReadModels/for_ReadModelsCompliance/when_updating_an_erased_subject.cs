// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance;

public class when_updating_an_erased_subject
{
    [Theory]
    [InlineData("")]
    [InlineData("new personal data")]
    public async Task should_store_the_erased_marker_without_recreating_the_key_or_stopping_the_update(string personalValue)
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},"retired":{"type":"boolean"}}}""");
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var compliance = new ReadModelsCompliance(manager, converter);
        var initial = await compliance.Apply("store", "Default", schema, "subject", State("original personal data", false));
        await keys.RecordErasureFor("store", "Default", "subject");
        await keys.DeleteFor("store", "Default", "subject");
        var releasedBeforeUpdate = await compliance.Release("store", "Default", schema, initial);

        var stored = await compliance.Apply("store", "Default", schema, "subject", State(personalValue, true));

        ((IDictionary<string, object?>)stored)["name"].ShouldEqual(((IDictionary<string, object?>)releasedBeforeUpdate)["name"]);
        ((IDictionary<string, object?>)stored)["name"].ShouldEqual(string.Empty);
        ((IDictionary<string, object?>)stored)["retired"].ShouldEqual(true);
        var released = await compliance.Release("store", "Default", schema, stored);
        ((IDictionary<string, object?>)released)["name"].ShouldEqual(string.Empty);
        (await keys.HasFor("store", "Default", "subject")).ShouldBeFalse();
        var error = await Assert.ThrowsAsync<SchemaMetadataActionFailed>(() => manager.Apply("store", "Default", schema, "subject", converter.ToJsonObject(State(personalValue, true), schema)));
        error.InnerException.ShouldBeOfExactType<EncryptionKeyErased>();

        await keys.AllowNewKeyFor("store", "Default", "subject");
        var newLifecycle = await compliance.Apply("store", "Default", schema, "subject", State("authorized personal data", false));
        var newReleased = await compliance.Release("store", "Default", schema, newLifecycle);
        ((IDictionary<string, object?>)newReleased)["name"].ShouldEqual("authorized personal data");
    }

    [Theory]
    [InlineData("compliance", "PII", false)]
    [InlineData("compliance", "Other", true)]
    [InlineData("security", "PII", true)]
    [InlineData("security", "Encrypted", true)]
    public async Task should_not_hide_other_handler_or_storage_failures(string category, string type, bool erasedError)
    {
        var schema = await JsonSchema.FromJsonAsync($$"""{"type":"object","properties":{"value":{"type":"string","{{category}}":[{"metadataType":"{{type}}","details":""}]} } }""");
        var handler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        handler.Category.Returns(category == "compliance" ? SchemaMetadataCategory.Compliance : SchemaMetadataCategory.Security);
        handler.Type.Returns((SchemaMetadataTypeName)type);
        Exception failure = erasedError ? new EncryptionKeyErased("subject", EncryptionKeyErasure.Covering(null, [])) : new IOException("key storage unavailable");
        handler.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>()).Returns(Task.FromException<JsonNode>(failure));
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);

        var error = await Assert.ThrowsAsync<SchemaMetadataActionFailed>(() => manager.ApplyToReadModel("store", "Default", schema, "subject", new JsonObject { ["value"] = "personal" }));

        Assert.Same(failure, error.InnerException);
    }

    static ExpandoObject State(string name, bool retired)
    {
        dynamic state = new ExpandoObject();
        state.name = name;
        state.retired = retired;
        return state;
    }
}
