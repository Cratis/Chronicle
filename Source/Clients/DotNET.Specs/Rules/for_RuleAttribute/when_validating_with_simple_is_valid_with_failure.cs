// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Rules.for_Rules.for_RuleAttribute;

public class when_validating_with_simple_is_valid_with_failure : given.a_validation_context
{
    const int ValueToValidate = 42;
    MyRuleAttribute _rule;
    ValidationResult _result;

    void Establish()
    {
        _rule = new()
        {
            simple_is_valid_return = false
        };
    }

    void Because()
    {
        try
        {
            _rule.Validate(ValueToValidate, _validationContext);
        }
        catch (ValidationException ex)
        {
            _result = ex.ValidationResult;
        }
    }

    [Fact] void should_not_call_complex_is_valid() => _rule.complex_is_valid_called.ShouldBeFalse();
    [Fact] void should_return_validation_result_with_error_message() => _result.ErrorMessage.ShouldEqual(MyRuleAttribute.ErrorMessage);
}
