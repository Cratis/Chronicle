// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.for_ComplianceMetadataResolver.when_asking_for_metadata_for_type;

public class and_there_are_none : Specification
{
    ComplianceMetadataResolver resolver;
    bool result;

    void Establish() =>
        resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>(Array.Empty<ICanProvideComplianceMetadataForType>()),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>(Array.Empty<ICanProvideComplianceMetadataForProperty>())
        );

    void Because() => result = resolver.HasMetadataFor(typeof(object));

    [Fact] void should_not_have_any() => result.ShouldBeFalse();
}
