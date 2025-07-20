// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_type;

public class and_there_is_metadata_and_details : given.a_provider
{
    const string Details = "These are the details";

    [PII]
    [ComplianceDetails(Details)]
    class MyType;

    ComplianceMetadata _result;

    void Because() => _result = provider.Provide(typeof(MyType));

    [Fact] void should_return_pii_metadata() => _result.MetadataType.ShouldEqual(ComplianceMetadataType.PII);
    [Fact] void should_return_metadata_with_details() => _result.Details.ShouldEqual(Details);
}
