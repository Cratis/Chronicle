// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class a_value_handler_and_a_type_with_a_compliant_list : Specification
{
    protected const string ListPropertyName = "scores";
    protected JsonSchema _schema;
    protected JsonObject _input;
    protected IJsonSchemaMetadataValueHandler _valueHandler;
    protected JsonSchemaMetadataManager _manager;

    protected readonly string _metadataType = "test-metadata-type";

    async Task Establish()
    {
        // Compliance metadata lives on the whole list property itself — a coarse [PII] list is blob-encrypted
        // to a single ciphertext string at rest, so the stored value for the property is a scalar string.
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "scores": {
                  "type": "array",
                  "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ],
                  "items": {
                    "type": "object",
                    "properties": {
                      "criterion": { "type": "string" },
                      "score": { "type": "integer" }
                    }
                  }
                }
              }
            }
            """);

        // The stored value is the blob ciphertext string, not an array.
        _input = new JsonObject { [ListPropertyName] = "cipher-text-blob" };

        _valueHandler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        _valueHandler.Type.Returns(_metadataType);
        _valueHandler.Category.Returns(SchemaMetadataCategory.Compliance);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(_valueHandler), NullLogger<JsonSchemaMetadataManager>.Instance);
    }
}
