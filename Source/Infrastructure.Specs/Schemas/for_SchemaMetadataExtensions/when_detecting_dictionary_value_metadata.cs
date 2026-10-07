// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_SchemaMetadataExtensions;

public class when_detecting_dictionary_value_metadata : Specification
{
    JsonSchema _schema;
    bool _compliance;
    bool _security;
    bool _matching;
    bool _nonMatching;

    async Task Establish() => _schema = await JsonSchema.FromJsonAsync(
        """
        {
          "type": "object",
          "additionalProperties": {
            "type": "array",
            "items": {
              "type": "string",
              "compliance": [{ "metadataType": "PII", "details": "" }],
              "security": [{ "metadataType": "EncryptedNamespace", "details": "" }]
            }
          }
        }
        """);

    void Because()
    {
        _compliance = _schema.HasSchemaMetadata(SchemaMetadataCategory.Compliance);
        _security = _schema.HasSchemaMetadata(SchemaMetadataCategory.Security);
        _matching = _schema.HasSchemaMetadata(SchemaMetadataCategory.Security, type => type == "EncryptedNamespace");
        _nonMatching = _schema.HasSchemaMetadata(SchemaMetadataCategory.Security, type => type == "EncryptedGlobal");
    }

    [Fact] void should_detect_compliance_in_dictionary_array_values() => _compliance.ShouldBeTrue();
    [Fact] void should_detect_security_in_dictionary_array_values() => _security.ShouldBeTrue();
    [Fact] void should_match_the_nested_metadata_type() => _matching.ShouldBeTrue();
    [Fact] void should_not_match_an_unrelated_metadata_type() => _nonMatching.ShouldBeFalse();
}
