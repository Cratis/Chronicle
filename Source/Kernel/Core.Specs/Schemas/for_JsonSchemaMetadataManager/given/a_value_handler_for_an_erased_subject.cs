// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class a_value_handler_for_an_erased_subject : Specification
{
    protected const string Identifier = "erased-subject";

    protected JsonSchema _schema;
    protected IJsonSchemaMetadataValueHandler _valueHandler;
    protected JsonSchemaMetadataManager _manager;
    protected JsonObject _input;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "name": { "type": "string", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] },
                "address": {
                  "type": "object",
                  "properties": {
                    "postalCode": { "type": "integer" },
                    "verified": { "type": "boolean" }
                  },
                  "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ]
                },
                "emails": {
                  "type": "array",
                  "items": { "type": "string", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] }
                },
                "status": { "type": "string" }
              }
            }
            """);

        _input = new JsonObject
        {
            ["name"] = "Ada Lovelace",
            ["address"] = new JsonObject { ["postalCode"] = 0, ["verified"] = false },
            ["emails"] = new JsonArray("ada@example.com"),
            ["status"] = "active"
        };

        _valueHandler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        _valueHandler.Type.Returns((SchemaMetadataTypeName)"test-metadata-type");
        _valueHandler.Category.Returns(SchemaMetadataCategory.Compliance);
        _valueHandler
            .Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns<Task<JsonNode>>(_ => throw new EncryptionKeyErased(Identifier, new EncryptionKeyErasure(EncryptionKeyRevision.Initial, [], false)));
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(_valueHandler), NullLogger<JsonSchemaMetadataManager>.Instance);
    }
}
