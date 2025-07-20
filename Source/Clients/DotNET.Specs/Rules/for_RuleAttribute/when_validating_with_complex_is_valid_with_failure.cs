// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Rules.for_Rules.for_RuleAttribute;

public class when_validating_with_complex_is_valid_with_failure : given.a_validation_context
{
    const int ValueToValidate = 42;
    MyRuleAttribute _rule;
    ValidationResult _result;
    ValidationResult _expected;

    void Establish()
    {
        _rule = new();
        _expected = new ValidationResult("Some error", ["Member1", "Member2"]);
        _rule.complex_is_value_return = _expected;
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

    [Fact] void should_return_expected_validation_result() => _result.ShouldEqual(_expected);
}
