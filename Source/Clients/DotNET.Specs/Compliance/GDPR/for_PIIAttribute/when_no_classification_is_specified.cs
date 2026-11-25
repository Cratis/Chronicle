// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIAttribute;

public class when_no_classification_is_specified : Specification
{
    PIIAttribute _result;

    void Because() => _result = typeof(PersonalData).GetCustomAttribute<PIIAttribute>()!;

    [Fact] void should_have_no_special_category() => _result.SpecialCategory.ShouldEqual(SpecialCategory.None);
    [Fact] void should_not_be_criminal() => _result.Criminal.ShouldBeFalse();
    [Fact] void should_not_be_an_operational_secret() => _result.OperationalSecret.ShouldBeFalse();
    [Fact] void should_have_empty_details() => _result.Details.ShouldEqual(string.Empty);

    [PII]
    class PersonalData;
}
