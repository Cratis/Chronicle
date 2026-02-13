// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager.given;

public class a_value_handler_and_a_type_with_one_property : a_type_with_one_property
{
    protected IJsonCompliancePropertyValueHandler _valueHandler;
    protected JsonComplianceManager _manager;

    protected readonly ComplianceMetadataType _metadataType = Guid.NewGuid();

    void Establish()
    {
        _schema.Properties.First()!.Value.ExtensionData = new Dictionary<string, object?>()
        {
            { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata(_metadataType, string.Empty) } }
        };

        _valueHandler = Substitute.For<IJsonCompliancePropertyValueHandler>();
        _valueHandler.Type.Returns(_metadataType);
        _manager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(_valueHandler));
    }
}
