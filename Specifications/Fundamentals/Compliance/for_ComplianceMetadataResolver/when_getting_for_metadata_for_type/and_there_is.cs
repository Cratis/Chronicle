// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Compliance.for_ComplianceMetadataResolver.when_getting_for_metadata_for_type
{
    public class and_there_is : Specification
    {
        ComplianceMetadataResolver resolver;

        ComplianceMetadata expected;
        ComplianceMetadata result;

        void Establish()
        {
            expected = new("134b380b-b298-4eda-bb81-5674ef326a32", "Some Details");
            var provider = new Mock<ICanProvideComplianceMetadataForType>();
            provider.Setup(_ => _.CanProvide(typeof(object))).Returns(true);
            provider.Setup(_ => _.Provide(typeof(object))).Returns(expected);

            resolver = new(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(new[] { provider.Object }),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>(Array.Empty<ICanProvideComplianceMetadataForProperty>())
            );
        }

        void Because() => result = resolver.GetMetadataFor(typeof(object));

        [Fact] void should_return_expected_metadata() => result.ShouldEqual(expected);
    }
}
