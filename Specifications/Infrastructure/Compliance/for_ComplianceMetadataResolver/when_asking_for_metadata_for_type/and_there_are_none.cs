// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_ComplianceMetadataResolver.when_asking_for_metadata_for_type;

public class and_there_are_none : Specification
{
    ComplianceMetadataResolver resolver;
    bool result;

    void Establish() =>
        resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>([]),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([])
        );

    void Because() => result = resolver.HasMetadataFor(typeof(object));

    [Fact] void should_not_have_any() => result.ShouldBeFalse();
}
