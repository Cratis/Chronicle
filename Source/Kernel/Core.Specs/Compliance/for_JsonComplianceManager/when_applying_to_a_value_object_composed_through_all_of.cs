// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

/// <summary>
/// Inheritance is expressed as an <c>allOf</c> reference to the base beside the type's own properties, and
/// resolving such a schema through <c>ActualTypeSchema</c> yields the base alone — without the members the type
/// declares itself. Anything deciding whether to descend has to agree with the lookup the walk then performs, or
/// a member classified right there is walked past and written in the clear beside a correctly encrypted sibling.
/// </summary>
public class when_applying_to_a_value_object_composed_through_all_of : Specification
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    const string EncryptedValue = "encrypted";
    const string Name = "Grace Hopper";

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
              "$defs": {
                "Base": { "type": "object" },
                "Person": {
                  "type": "object",
                  "allOf": [ { "$ref": "#/$defs/Base" } ],
                  "properties": {
                    "name": { "type": "string", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] }
                  }
                }
              },
              "properties": {
                "person": { "$ref": "#/$defs/Person" }
              }
            }
            """);

        _input = new JsonObject
        {
            ["person"] = new JsonObject { ["name"] = Name }
        };

        _valueHandler = Substitute.For<IJsonCompliancePropertyValueHandler>();
        _valueHandler.Type.Returns(_metadataType);
        _valueHandler.Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>()).Returns(_ => Task.FromResult<JsonNode>(JsonValue.Create(EncryptedValue)));
        _manager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(_valueHandler), NullLogger<JsonComplianceManager>.Instance);
    }

    async Task Because() => _result = await _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_encrypt_the_declared_member() => _result["person"]!["name"]!.GetValue<string>().ShouldEqual(EncryptedValue);
    [Fact] void should_not_leave_it_in_the_clear() => _result["person"]!["name"]!.GetValue<string>().ShouldNotEqual(Name);
}
