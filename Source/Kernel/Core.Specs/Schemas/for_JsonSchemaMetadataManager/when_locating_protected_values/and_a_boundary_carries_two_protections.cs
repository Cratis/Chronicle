// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_locating_protected_values;

public class and_a_boundary_carries_two_protections : Specification
{
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    Exception _error;

    async Task Establish()
    {
        var first = Substitute.For<IJsonSchemaMetadataValueHandler>();
        first.Category.Returns(SchemaMetadataCategory.Compliance);
        first.Type.Returns((SchemaMetadataTypeName)"first");
        var second = Substitute.For<IJsonSchemaMetadataValueHandler>();
        second.Category.Returns(SchemaMetadataCategory.Compliance);
        second.Type.Returns((SchemaMetadataTypeName)"second");
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(first, second), NullLogger<JsonSchemaMetadataManager>.Instance);
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string","compliance":[{"metadataType":"first","details":""},{"metadataType":"second","details":""}]}}}""");
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.LocateProtectedValues(_schema, "subject", new JsonObject { ["value"] = "private" }));

    [Fact] void should_refuse_the_ambiguous_boundary() => _error.ShouldBeOfExactType<ProtectedBoundaryUnsupported>();
    [Fact] void should_name_the_unsupported_path() => ((ProtectedBoundaryUnsupported)_error).Path.ShouldEqual("value");
}
