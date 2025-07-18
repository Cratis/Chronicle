// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Rules.for_Rules.for_RuleAttribute;

public class when_validating_with_model_key : given.a_validation_context
{
    const int ValueToValidate = 42;
    MyRuleAttribute _rule;

    void Establish()
    {
        _rule = new()
        {
            IsModelKey = true
        };
    }

    void Because() => _rule.Validate(ValueToValidate, _validationContext);

    [Fact] void should_project_to_rule_with_value_as_model_key() => _rules.Received(1).ProjectTo(_rule, ValueToValidate);
}
