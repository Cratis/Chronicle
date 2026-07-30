// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager.given;

public class a_value_handler_and_a_type_with_a_compliant_member_on_a_nested_object : Specification
{
    protected JsonSchema _schema;
    protected IJsonCompliancePropertyValueHandler _valueHandler;
    protected JsonComplianceManager _manager;

    protected readonly ComplianceMetadataType _metadataType = "test-metadata-type";

    async Task Establish()
    {
        // Compliance metadata lives on a leaf inside a nested value object — the shape the schema generator
        // produces for a [PII] concept held by a value object, and for a [PII] marker on the value object itself.
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "id": { "type": "string" },
                "dateOfBirth": {
                  "type": "object",
                  "properties": {
                    "dateOfBirth": { "type": "string", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] },
                    "verifiedBy": { "type": "string" }
                  }
                }
              }
            }
            """);

        _valueHandler = Substitute.For<IJsonCompliancePropertyValueHandler>();
        _valueHandler.Type.Returns(_metadataType);
        _manager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(_valueHandler), NullLogger<JsonComplianceManager>.Instance);
    }
}
