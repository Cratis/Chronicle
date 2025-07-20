// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Strings;

namespace Cratis.Chronicle.Schemas.for_ComplianceMetadataSchemaProcessor;

public class when_processing_type_with_metadata_and_two_properties_with_metadata : given.a_processor_and_a_context_for<TypeWithProperties>
{
    ComplianceMetadata _firstTypeMetadata;
    ComplianceMetadata _secondTypeMetadata;
    ComplianceMetadata _firstPropertyFirstMetadata;
    ComplianceMetadata _firstPropertySecondMetadata;
    ComplianceMetadata _secondPropertyFirstMetadata;
    ComplianceMetadata _secondPropertySecondMetadata;

    void Establish()
    {
        _firstTypeMetadata = new("5a9536de-a1e1-47cb-9146-96a7113bbf60", "First metadata details");
        _secondTypeMetadata = new("7fec8e35-dfc1-4718-9b77-4095d081cb93", "Second metadata details");
        _resolver.HasMetadataFor(typeof(TypeWithProperties)).Returns(true);
        _resolver.GetMetadataFor(typeof(TypeWithProperties)).Returns([_firstTypeMetadata, _secondTypeMetadata]);

        _firstPropertyFirstMetadata = new("731547a4-5969-435f-84a5-57760b0fb7c7", "First property first metadata details");
        _firstPropertySecondMetadata = new("0e89d0a8-2943-4c79-be9c-94b446775687", "First property second metadata details");
        _resolver.HasMetadataFor(TypeWithProperties.FirstProperty).Returns(true);
        _resolver.GetMetadataFor(TypeWithProperties.FirstProperty).Returns([_firstPropertyFirstMetadata, _firstPropertySecondMetadata]);

        _secondPropertyFirstMetadata = new("cb084784-20a7-4009-a0a7-66a6957adf87", "Second property first metadata details");
        _secondPropertySecondMetadata = new("b96dedad-d2e9-4559-a812-beedf4ef1356", "Second property second metadata details");
        _resolver.HasMetadataFor(TypeWithProperties.SecondProperty).Returns(true);
        _resolver.GetMetadataFor(TypeWithProperties.SecondProperty).Returns([_secondPropertyFirstMetadata, _secondPropertySecondMetadata]);
    }

    void Because() => _processor.Process(_context);

    [Fact] void should_add_first_type_metadata_with_correct_type() => GetMetadata()[0].metadataType.ShouldEqual(_firstTypeMetadata.MetadataType.Value);
    [Fact] void should_add_first_type_metadata_with_correct_details() => GetMetadata()[0].details.ShouldEqual(_firstTypeMetadata.Details);
    [Fact] void should_add_second_type_metadata_with_correct_type() => GetMetadata()[1].metadataType.ShouldEqual(_secondTypeMetadata.MetadataType.Value);
    [Fact] void should_add_second_type_metadata_with_correct_details() => GetMetadata()[1].details.ShouldEqual(_secondTypeMetadata.Details);
    [Fact] void should_add_first_property_first_metadata_with_correct_type() => GetMetadataForProperty(nameof(TypeWithProperties.First))[0].metadataType.ShouldEqual(_firstPropertyFirstMetadata.MetadataType.Value);
    [Fact] void should_add_first_property_first_type_metadata_with_correct_details() => GetMetadataForProperty(nameof(TypeWithProperties.First))[0].details.ShouldEqual(_firstPropertyFirstMetadata.Details);
    [Fact] void should_add_first_property_second_metadata_with_correct_type() => GetMetadataForProperty(nameof(TypeWithProperties.First))[1].metadataType.ShouldEqual(_firstPropertySecondMetadata.MetadataType.Value);
    [Fact] void should_add_first_property_second_type_metadata_with_correct_details() => GetMetadataForProperty(nameof(TypeWithProperties.First))[1].details.ShouldEqual(_firstPropertySecondMetadata.Details);
    [Fact] void should_add_second_property_first_metadata_with_correct_type() => GetMetadataForProperty(nameof(TypeWithProperties.Second))[0].metadataType.ShouldEqual(_secondPropertyFirstMetadata.MetadataType.Value);
    [Fact] void should_add_second_property_first_type_metadata_with_correct_details() => GetMetadataForProperty(nameof(TypeWithProperties.Second))[0].details.ShouldEqual(_secondPropertyFirstMetadata.Details);
    [Fact] void should_add_second_property_second_metadata_with_correct_type() => GetMetadataForProperty(nameof(TypeWithProperties.Second))[1].metadataType.ShouldEqual(_secondPropertySecondMetadata.MetadataType.Value);
    [Fact] void should_add_second_property_second_type_metadata_with_correct_details() => GetMetadataForProperty(nameof(TypeWithProperties.Second))[1].details.ShouldEqual(_secondPropertySecondMetadata.Details);

    ComplianceSchemaMetadata[] GetMetadata() => ((IEnumerable<ComplianceSchemaMetadata>)_context.Schema.ExtensionData[ComplianceJsonSchemaExtensions.ComplianceKey]).ToArray();
    ComplianceSchemaMetadata[] GetMetadataForProperty(string property)
    {
        var propertySchema = _context.Schema.Properties.Single(_ => _.Key == property.ToCamelCase()).Value;
        return ((IEnumerable<ComplianceSchemaMetadata>)propertySchema.ExtensionData[ComplianceJsonSchemaExtensions.ComplianceKey]).ToArray();
    }
}
