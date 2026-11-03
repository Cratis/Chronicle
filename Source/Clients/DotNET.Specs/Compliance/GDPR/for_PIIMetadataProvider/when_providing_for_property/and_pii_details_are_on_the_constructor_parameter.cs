// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_providing_for_property;

public class and_pii_details_are_on_the_constructor_parameter : given.a_provider
{
    record Person([PII("Constructor details")] string Name);

    ComplianceMetadata _result;

    void Because() => _result = provider.Provide(typeof(Person).GetProperty(nameof(Person.Name))!);

    [Fact] void should_return_constructor_details() => _result.Details.ShouldEqual("Constructor details");
}
