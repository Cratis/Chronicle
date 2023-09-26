// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_Rules.for_RuleAttribute;

public class when_validating_without_model_key : given.a_validation_context
{
    const int value_to_validate = 42;
    MyRuleAttribute rule;

    void Establish()
    {
        rule = new()
        {
            IsModelKey = false
        };
    }

    void Because() => rule.Validate(value_to_validate, validation_context);

    [Fact] void should_project_to_rule_without_model_key() => rules.Verify(_ => _.ProjectTo(rule, null));
}
