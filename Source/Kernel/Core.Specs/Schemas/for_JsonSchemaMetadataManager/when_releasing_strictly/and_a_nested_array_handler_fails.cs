// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_strictly;

public class and_a_nested_array_handler_fails : Specification
{
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    JsonObject _content;
    Exception _error;
    string _original;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"profile":{"type":"object","properties":{
              "values":{"type":"array","items":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}
            }}}}
            """);
        _content = JsonNode.Parse("""{"profile":{"values":["protected-value"]}}""")!.AsObject();
        _original = _content.ToJsonString();
        var handler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        handler.Category.Returns(SchemaMetadataCategory.Compliance);
        handler.Type.Returns((SchemaMetadataTypeName)"PII");
        handler.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>()).ThrowsAsync(new MissingEncryptionKey("owner"));
        _manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.ReleaseStrict("store", "namespace", _schema, "owner", _content));

    [Fact] void should_fail_the_entire_release() => _error.ShouldBeOfExactType<SchemaMetadataActionFailed>();
    [Fact] void should_leave_the_input_unchanged() => _content.ToJsonString().ShouldEqual(_original);
}
