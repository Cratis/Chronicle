// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Aksio.Cratis.Rules.for_Rules.for_RuleAttribute;

public class when_validating_with_complex_is_valid_with_failure : given.a_validation_context
{
    const int value_to_validate = 42;
    MyRuleAttribute rule;
    ValidationResult result;
    ValidationResult expected;

    void Establish()
    {
        rule = new();
        expected = new ValidationResult("Some error", new[] { "Member1", "Member2" });
        rule.complex_is_value_return = expected;
    }

    void Because()
    {
        try
        {
            rule.Validate(value_to_validate, validation_context);
        }
        catch (ValidationException ex)
        {
            result = ex.ValidationResult;
        }
    }

    [Fact] void should_return_expected_validation_result() => result.ShouldEqual(expected);
}
