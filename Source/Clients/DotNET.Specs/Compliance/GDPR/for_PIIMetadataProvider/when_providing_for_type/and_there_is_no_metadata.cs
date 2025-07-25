// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_type;

public class and_there_is_no_metadata : given.a_provider
{
    Exception _result;
    void Because() => _result = Catch.Exception(() => provider.Provide(typeof(object)));

    [Fact] void should_throw_no_compliance_metadata_for_type() => _result.ShouldBeOfExactType<NoComplianceMetadataForType>();
}
