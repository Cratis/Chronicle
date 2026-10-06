// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_releasing_legacy_scalar_text : Specification
{
    const string ReleasedText = "Jane-private-value";
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    JsonObject _result;
    ILogger<JsonSchemaMetadataManager> _logger;

    void Establish()
    {
        _schema = JsonSchema.FromJson(
            """
            {
              "type": "object",
              "properties": {
                "number": { "type": "integer", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "boolean": { "type": "boolean", "compliance": [{ "metadataType": "PII", "details": "" }] }
              }
            }
            """);
        var handler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        handler.Category.Returns(SchemaMetadataCategory.Compliance);
        handler.Type.Returns((SchemaMetadataTypeName)"PII");
        handler.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns(call => Task.FromResult<JsonNode>(JsonValue.Create(call.Arg<JsonNode>().GetValue<string>())));
        _logger = Substitute.For<ILogger<JsonSchemaMetadataManager>>();
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), _logger);
    }

    async Task Because() => _result = await _manager.Release(
        EventStoreName.NotSet,
        EventStoreNamespaceName.Default,
        _schema,
        "owner",
        new JsonObject { ["number"] = ReleasedText, ["boolean"] = "42" });

    [Fact] void should_preserve_unparseable_scalar_text_for_legacy_conversion() => _result["number"]!.GetValue<string>().ShouldEqual(ReleasedText);
    [Fact] void should_not_restore_a_number_as_a_boolean() => _result["boolean"]!.GetValue<string>().ShouldEqual("42");
    [Fact] void should_not_log_decrypted_parse_failures() => _logger.ReceivedCalls().ShouldBeEmpty();
}
