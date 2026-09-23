// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

/// <summary>
/// A dictionary declares the shape of its values but never their names, so the walk cannot find a key of it in
/// the schema and fails. That failure is the contract, not an oversight: the alternative is writing values that
/// were classified as personal into the store in the clear, which applying must never do. Chronicle does not
/// classify values inside a dictionary today, and until it does, this has to keep failing where it can be seen.
/// </summary>
public class when_applying_to_a_dictionary_of_compliant_values : Specification
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";

    readonly string _metadataType = "test-metadata-type";

    JsonSchema _schema;
    JsonObject _input;
    JsonSchemaMetadataManager _manager;
    Exception _exception;

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
        valueHandler.Type.Returns(_metadataType);
        valueHandler.Category.Returns(SchemaMetadataCategory.Compliance);
        valueHandler.Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>()).Returns(_ => Task.FromResult<JsonNode>(JsonValue.Create("encrypted")));
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(valueHandler), NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    async Task Because() => _exception = await Catch.Exception(() => _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input));

    [Fact] void should_fail_rather_than_store_the_values_unprotected() => _exception.ShouldBeOfExactType<SchemaPropertyNotFoundInSchema>();
    [Fact] void should_name_the_key_it_could_not_resolve() => _exception.Message.ShouldContain("contacts.home");
}
