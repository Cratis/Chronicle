// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

/// <summary>
/// Skipping the descent into a geospatial value must not skip the value itself. A location that genuinely is
/// personal carries the marker on its own property, and is then encrypted as a whole like any other container —
/// otherwise a value someone explicitly classified would be written in the clear.
/// </summary>
public class when_applying_to_a_marked_geospatial_value : Specification
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    const string EncryptedValue = "encrypted";

    readonly ComplianceMetadataType _metadataType = "test-metadata-type";

    JsonSchema _schema;
    JsonObject _input;
    IJsonCompliancePropertyValueHandler _valueHandler;
    JsonComplianceManager _manager;
    JsonObject _result;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "homeLocation": { "type": "object", "format": "point", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] }
              }
            }
            """);

        _input = new JsonObject
        {
            ["homeLocation"] = new JsonObject
            {
                ["type"] = "Point",
                ["coordinates"] = new JsonArray(10.7522, 59.9139)
            }
        };

        _valueHandler = Substitute.For<IJsonCompliancePropertyValueHandler>();
        _valueHandler.Type.Returns(_metadataType);
        _valueHandler.Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>()).Returns(_ => Task.FromResult<JsonNode>(JsonValue.Create(EncryptedValue)));
        _manager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(_valueHandler), NullLogger<JsonComplianceManager>.Instance);
    }

    async Task Because() => _result = await _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_encrypt_the_whole_value() => _result["homeLocation"]!.GetValue<string>().ShouldEqual(EncryptedValue);
    [Fact] void should_have_handed_the_value_to_the_handler() => _valueHandler.Received(1).Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>());
}
