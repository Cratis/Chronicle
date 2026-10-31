// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_property;

public class and_both_are_given : given.a_provider
{
    class Person
    {
        [PII("PII details")]
        [ComplianceDetails("Explicit details")]
        public string Name { get; set; }
    }

    ComplianceMetadata _result;

    void Because() => _result = provider.Provide(typeof(Person).GetProperty(nameof(Person.Name))!);

    [Fact] void should_prefer_compliance_details() => _result.Details.ShouldEqual("Explicit details");
}
