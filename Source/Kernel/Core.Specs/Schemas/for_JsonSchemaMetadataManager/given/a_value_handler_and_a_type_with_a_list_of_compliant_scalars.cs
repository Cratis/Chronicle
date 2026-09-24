// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class a_value_handler_and_a_type_with_a_list_of_compliant_scalars : Specification
{
    protected JsonSchema _schema;
    protected IJsonSchemaMetadataValueHandler _valueHandler;
    protected JsonSchemaMetadataManager _manager;

    protected readonly string _metadataType = "test-metadata-type";

    async Task Establish()
    {
        // Compliance metadata lives on the array's element type — the IReadOnlyList<Email> shape.
        // Built from JSON so it matches how a persisted schema is loaded at runtime.
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "emails": {
                  "type": "array",
                  "items": { "type": "string", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] }
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
