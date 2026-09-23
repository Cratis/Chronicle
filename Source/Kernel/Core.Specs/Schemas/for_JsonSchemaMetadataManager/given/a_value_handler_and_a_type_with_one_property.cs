// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class a_value_handler_and_a_type_with_one_property : a_type_with_one_property
{
    protected IJsonSchemaMetadataValueHandler _valueHandler;
    protected JsonSchemaMetadataManager _manager;

    protected readonly string _metadataType = "test-metadata-type";

    void Establish()
    {
        _schema.Properties.First().Value.ExtensionData = new Dictionary<string, object?>()
        {
            { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata(_metadataType, string.Empty) } }
        };

        _valueHandler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        _valueHandler.Type.Returns((SchemaMetadataTypeName)_metadataType);
        _valueHandler.Category.Returns(SchemaMetadataCategory.Compliance);
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(_valueHandler),
            NullLogger<JsonSchemaMetadataManager>.Instance);
    }
}
