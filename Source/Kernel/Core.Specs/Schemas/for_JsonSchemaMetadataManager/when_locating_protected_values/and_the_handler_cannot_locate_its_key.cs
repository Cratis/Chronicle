// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_locating_protected_values;

public class and_the_handler_cannot_locate_its_key : Specification
{
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    Exception _error;

    async Task Establish()
    {
        var handler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        handler.Category.Returns(SchemaMetadataCategory.Compliance);
        handler.Type.Returns((SchemaMetadataTypeName)"custom");
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string","compliance":[{"metadataType":"custom","details":""}]}}}""");
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.LocateProtectedValues(_schema, "subject", new JsonObject { ["value"] = "private" }));

    [Fact] void should_refuse_the_unlocatable_boundary() => _error.ShouldBeOfExactType<ProtectedBoundaryUnsupported>();
    [Fact] void should_name_the_unsupported_path() => ((ProtectedBoundaryUnsupported)_error).Path.ShouldEqual("value");
}
