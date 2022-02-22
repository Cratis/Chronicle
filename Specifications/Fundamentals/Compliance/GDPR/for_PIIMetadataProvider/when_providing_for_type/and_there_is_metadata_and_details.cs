// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_type;

public class and_there_is_metadata_and_details : given.a_provider
{
    const string details = "These are the details";

    [PII]
    [ComplianceDetails(details)]
    class MyType { }

    ComplianceMetadata result;

    void Because() => result = provider.Provide(typeof(MyType));

    [Fact] void should_return_pii_metadata() => result.MetadataType.ShouldEqual(ComplianceMetadataType.PII);
    [Fact] void should_return_metadata_with_details() => result.Details.ShouldEqual(details);
}
