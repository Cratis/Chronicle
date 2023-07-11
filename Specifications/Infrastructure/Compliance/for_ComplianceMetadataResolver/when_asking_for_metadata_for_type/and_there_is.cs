// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.for_ComplianceMetadataResolver.when_asking_for_metadata_for_type;

public class and_there_is : Specification
{
    ComplianceMetadataResolver resolver;
    bool result;

    void Establish()
    {
        var provider = new Mock<ICanProvideComplianceMetadataForType>();
        provider.Setup(_ => _.CanProvide(typeof(object))).Returns(true);

        resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>(new[] { provider.Object }),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>(Array.Empty<ICanProvideComplianceMetadataForProperty>())
        );
    }

    void Because() => result = resolver.HasMetadataFor(typeof(object));

    [Fact] void should_have() => result.ShouldBeTrue();
}
