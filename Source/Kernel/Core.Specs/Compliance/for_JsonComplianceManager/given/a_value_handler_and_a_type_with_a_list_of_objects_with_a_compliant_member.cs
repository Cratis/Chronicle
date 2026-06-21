// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager.given;

public class a_value_handler_and_a_type_with_a_list_of_objects_with_a_compliant_member : Specification
{
    protected JsonSchema _schema;
    protected IJsonCompliancePropertyValueHandler _valueHandler;
    protected JsonComplianceManager _manager;

    protected readonly ComplianceMetadataType _metadataType = "test-metadata-type";

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

        _valueHandler = Substitute.For<IJsonCompliancePropertyValueHandler>();
        _valueHandler.Type.Returns(_metadataType);
        _manager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(_valueHandler));
    }
}
