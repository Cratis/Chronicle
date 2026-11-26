// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIAttribute;

public class when_classification_is_specified : Specification
{
    PIIAttribute _result;

    void Because() => _result = typeof(PersonalData).GetCustomAttribute<PIIAttribute>()!;

    [Fact] void should_have_the_special_category() => _result.SpecialCategory.ShouldEqual(SpecialCategory.Health);
    [Fact] void should_be_criminal() => _result.Criminal.ShouldBeTrue();
    [Fact] void should_be_an_operational_secret() => _result.OperationalSecret.ShouldBeTrue();
    [Fact] void should_preserve_details() => _result.Details.ShouldEqual("Sensitive personal data");

    [PII("Sensitive personal data", SpecialCategory = SpecialCategory.Health, Criminal = true, OperationalSecret = true)]
    class PersonalData;
}
