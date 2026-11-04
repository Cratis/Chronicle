// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_applying_to_read_model;

public class and_a_non_pii_handler_throws_an_erasure : Specification
{
    readonly EncryptionKeyErased _failure = new("subject", EncryptionKeyErasure.Covering(null, []));
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    Exception _error;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string","security":[{"metadataType":"Encrypted","details":""}]}}}""");
        var handler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        handler.Category.Returns(SchemaMetadataCategory.Security);
        handler.Type.Returns((SchemaMetadataTypeName)"Encrypted");
        handler.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>()).Returns(Task.FromException<JsonNode>(_failure));
        _manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.ApplyToReadModel("store", "Default", _schema, "subject", new JsonObject { ["value"] = "protected" }));

    [Fact] void should_wrap_the_failure() => _error.ShouldBeOfExactType<SchemaMetadataActionFailed>();
    [Fact] void should_not_treat_a_non_pii_erasure_as_an_empty_marker() => _error.InnerException.ShouldBeSame(_failure);
}
