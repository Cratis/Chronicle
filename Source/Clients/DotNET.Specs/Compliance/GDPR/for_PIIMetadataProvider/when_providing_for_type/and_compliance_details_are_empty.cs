// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_type;

public class and_compliance_details_are_empty : given.a_provider
{
    [PII("Concept details")]
    [ComplianceDetails("")]
    record Name(string Value) : ConceptAs<string>(Value);

    ComplianceMetadata _result;

    void Because() => _result = provider.Provide(typeof(Name));

    [Fact] void should_fall_back_to_pii_details() => _result.Details.ShouldEqual("Concept details");
}
