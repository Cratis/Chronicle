// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Aksio.Cratis.Rules.for_Rules.for_RuleAttribute;

public class when_validating_with_simple_is_valid_with_failure : given.a_validation_context
{
    const int value_to_validate = 42;
    MyRuleAttribute rule;
    ValidationResult result;

    void Establish()
    {
        rule = new()
        {
            simple_is_valid_return = false
        };
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

    [Fact] void should_not_call_complex_is_valid() => rule.complex_is_valid_called.ShouldBeFalse();
    [Fact] void should_return_validation_result_with_error_message() => result.ErrorMessage.ShouldEqual(MyRuleAttribute.error_message);
}
