// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_ComplianceMetadataResolver.when_asking_for_metadata_for_type;

public class and_there_are_none : Specification
{
    ComplianceMetadataResolver _resolver;
    bool _result;

    void Establish() =>
        _resolver = new(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>([]),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([])
        );

    void Because() => _result = _resolver.HasMetadataFor(typeof(object));

    [Fact] void should_not_have_any() => _result.ShouldBeFalse();
}
