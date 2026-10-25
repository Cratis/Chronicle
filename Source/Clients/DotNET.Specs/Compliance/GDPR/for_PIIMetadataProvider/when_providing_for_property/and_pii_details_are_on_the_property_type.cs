// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_property;

public class and_pii_details_are_on_the_property_type : given.a_provider
{
    [PII("Concept details")]
    record Name(string Value) : ConceptAs<string>(Value);

    record Person(Name Name);

    ComplianceMetadata _result;

    void Because() => _result = provider.Provide(typeof(Person).GetProperty(nameof(Person.Name))!);

    [Fact] void should_return_concept_details() => _result.Details.ShouldEqual("Concept details");
}
