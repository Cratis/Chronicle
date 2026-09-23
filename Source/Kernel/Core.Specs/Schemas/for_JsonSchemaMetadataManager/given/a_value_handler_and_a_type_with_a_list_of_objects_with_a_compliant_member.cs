// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class a_value_handler_and_a_type_with_a_list_of_objects_with_a_compliant_member : Specification
{
    protected JsonSchema _schema;
    protected IJsonSchemaMetadataValueHandler _valueHandler;
    protected JsonSchemaMetadataManager _manager;

    protected readonly string _metadataType = "test-metadata-type";

    async Task Establish()
    {
        // Compliance metadata lives on a member of the objects held in the list.
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "contacts": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "email": { "type": "string", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] }
                    }
                  }
                }
              }
            }
            """);

        _valueHandler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        _valueHandler.Type.Returns((SchemaMetadataTypeName)_metadataType);
        _valueHandler.Category.Returns(SchemaMetadataCategory.Compliance);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(_valueHandler), NullLogger<JsonSchemaMetadataManager>.Instance);
    }
}
