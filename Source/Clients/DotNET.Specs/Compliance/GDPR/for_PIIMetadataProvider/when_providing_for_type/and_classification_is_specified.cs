// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_type;

public class and_classification_is_specified : given.a_provider
{
    ComplianceMetadata _result;

    void Because() => _result = provider.Provide(typeof(PersonalData));

    [Fact] void should_provide_only_pii_metadata_and_details() => _result.ShouldEqual(new ComplianceMetadata(ComplianceMetadataType.PII, "Sensitive personal data"));

    [PII("Sensitive personal data", SpecialCategory = SpecialCategory.Health, Criminal = true, OperationalSecret = true)]
    record PersonalData(string Value) : ConceptAs<string>(Value);
}
