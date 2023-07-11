// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidation.Results;

namespace Aksio.Cratis.Rules.for_Rules.for_RulesModelValidator.given;

public class two_rule_sets : Specification
{
    protected Mock<IRule> first_rule_set;
    protected Mock<IValidator> first_rule_set_as_validator;
    protected Mock<IRule> second_rule_set;
    protected Mock<IValidator> second_rule_set_as_validator;
    protected Mock<IRules> rules;
    protected RuleModelValidator validator;
    protected ValidationResult first_rule_set_validation_result;
    protected ValidationResult second_rule_set_validation_result;

    void Establish()
    {
        first_rule_set = new Mock<IRule>();
        first_rule_set_as_validator = first_rule_set.As<IValidator>();
        second_rule_set = new Mock<IRule>();
        second_rule_set_as_validator = second_rule_set.As<IValidator>();

        rules = new();

        validator = new(new[] { first_rule_set.Object, second_rule_set.Object }, rules.Object);

        first_rule_set_validation_result = new ValidationResult(new[]
        {
            new ValidationFailure("FirstRuleSetFirstProp", "First RuleSet First Prop Failed"),
            new ValidationFailure("FirstRuleSetSecondProp", "First RuleSet Second Prop Failed"),
        });

        second_rule_set_validation_result = new ValidationResult(new[]
        {
            new ValidationFailure("SecondRuleSetFirstProp", "Second RuleSet First Prop Failed"),
            new ValidationFailure("SecondRuleSetSecondProp", "Second RuleSet Second Prop Failed"),
        });

        first_rule_set_as_validator.Setup(_ => _.Validate(IsAny<IValidationContext>())).Returns(first_rule_set_validation_result);
        second_rule_set_as_validator.Setup(_ => _.Validate(IsAny<IValidationContext>())).Returns(second_rule_set_validation_result);
    }
}
