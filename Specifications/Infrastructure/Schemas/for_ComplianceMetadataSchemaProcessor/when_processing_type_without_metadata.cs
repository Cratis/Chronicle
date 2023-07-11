// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Schemas.for_ComplianceMetadataSchemaProcessor;

public class when_processing_type_without_metadata : given.a_processor_and_a_context_for<TypeWithoutProperties>
{
    void Because() => processor.Process(context);

    [Fact] void should_contain_compliance_info() => context.Schema.ExtensionData?.Keys.ShouldNotContain(JsonSchemaGenerator.ComplianceKey);
}
