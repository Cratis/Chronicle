// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

/// <summary>
/// A dictionary's dynamic keys resolve against its additional-properties schema, so classified values are
/// protected even though their names are not declared in the schema.
/// </summary>
public class when_applying_to_a_dictionary_of_compliant_values : Specification
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";

    readonly string _metadataType = "test-metadata-type";

    JsonSchema _schema;
    JsonObject _input;
    JsonSchemaMetadataManager _manager;
    JsonObject _result;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "name": { "type": "string", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] },
                "contacts": {
                  "type": "object",
                  "additionalProperties": { "type": "string", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] }
                }
              }
            }
            """);

        _input = new JsonObject
        {
            ["name"] = "Ada Lovelace",
            ["contacts"] = new JsonObject { ["home"] = "ada@example.com" }
        };

        var valueHandler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        valueHandler.Type.Returns((SchemaMetadataTypeName)_metadataType);
        valueHandler.Category.Returns(SchemaMetadataCategory.Compliance);
        valueHandler.Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>()).Returns(_ => Task.FromResult<JsonNode>(JsonValue.Create("encrypted")));
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(valueHandler), NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    async Task Because() => _result = await _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_protect_the_declared_property() => _result["name"]!.GetValue<string>().ShouldEqual("encrypted");
    [Fact] void should_protect_the_dynamic_dictionary_value() => _result["contacts"]!["home"]!.GetValue<string>().ShouldEqual("encrypted");
}
