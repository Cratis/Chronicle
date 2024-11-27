// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_ComplianceMetadataResolver.when_asking_for_metadata_for_type;

public class and_there_is : Specification
{
    ComplianceMetadataResolver _resolver;
    bool _result;

    void Establish()
    {
        var provider = Substitute.For<ICanProvideComplianceMetadataForType>();
        provider.CanProvide(typeof(object)).Returns(true);

        _resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>([provider]),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([])
        );
    }

    void Because() => _result = _resolver.HasMetadataFor(typeof(object));

    [Fact] void should_have() => _result.ShouldBeTrue();
}
