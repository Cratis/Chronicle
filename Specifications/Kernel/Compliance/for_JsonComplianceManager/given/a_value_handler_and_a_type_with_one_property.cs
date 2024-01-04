// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Kernel.Compliance.for_JsonComplianceManager.given;

public class a_value_handler_and_a_type_with_one_property : a_type_with_one_property
{
    protected Mock<IJsonCompliancePropertyValueHandler> value_handler;
    protected JsonComplianceManager manager;

    protected readonly Guid metadata_type = Guid.NewGuid();

    void Establish()
    {
        schema.Properties.First()!.Value.ExtensionData = new Dictionary<string, object>()
            {
                { JsonSchemaGenerator.ComplianceKey, new[] { new ComplianceSchemaMetadata(metadata_type, string.Empty) } }
            };

        value_handler = new();
        value_handler.SetupGet(_ => _.Type).Returns(metadata_type);
        manager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(value_handler.Object));
    }
}
