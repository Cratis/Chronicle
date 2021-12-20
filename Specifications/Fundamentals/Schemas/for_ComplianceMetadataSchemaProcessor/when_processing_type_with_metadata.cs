// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;

namespace Cratis.Schemas.for_ComplianceMetadataSchemaProcessor
{
    public class when_processing_type_with_metadata : given.a_processor_and_a_context_for<TypeWithoutProperties>
    {
        ComplianceMetadata metadata;

        void Establish()
        {
            metadata = new("5a9536de-a1e1-47cb-9146-96a7113bbf60", "Compliance metadata details");
            resolver.Setup(_ => _.HasMetadataFor(typeof(TypeWithoutProperties))).Returns(true);
            resolver.Setup(_ => _.GetMetadataFor(typeof(TypeWithoutProperties))).Returns(metadata);
        }

        void Because() => processor.Process(context);

        [Fact] void should_add_metadata_with_correct_type() => GetFirstMetadata().type.ShouldEqual(metadata.Type.Value);
        [Fact] void should_add_metadata_with_correct_details() => GetFirstMetadata().details.ShouldEqual(metadata.Details);

        ComplianceMetadataSchemaProcessor.Metadata GetFirstMetadata() => (dynamic)((IEnumerable<ComplianceMetadataSchemaProcessor.Metadata>)context.Schema.ExtensionData[ComplianceMetadataSchemaProcessor.ComplianceKey]).First();
    }
}
