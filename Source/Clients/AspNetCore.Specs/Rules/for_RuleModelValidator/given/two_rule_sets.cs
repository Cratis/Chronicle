// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Rules;
using FluentValidation;
using FluentValidation.Results;

namespace Cratis.Chronicle.AspNetCore.Rules.for_Rules.for_RulesModelValidator.given;

public class two_rule_sets : Specification
{
    protected IRule _firstRuleSet;
    protected IValidator _firstRuleSetAsValidator;
    protected IRule _secondRuleSet;
    protected IValidator _secondRuleSetAsValidator;
    protected IRules _rules;
    protected RuleModelValidator _validator;
    protected ValidationResult _firstRuleSetValidationResult;
    protected ValidationResult _secondRuleSetValidationResult;

    void Establish()
    {
        _firstRuleSet = Substitute.For<IRule, IValidator>();
        _firstRuleSetAsValidator = _firstRuleSet as IValidator;
        _secondRuleSet = Substitute.For<IRule, IValidator>();
        _secondRuleSetAsValidator = _secondRuleSet as IValidator;

        _rules = Substitute.For<IRules>();

        _validator = new([_firstRuleSet, _secondRuleSet], _rules);

        _firstRuleSetValidationResult = new ValidationResult(
        [
            new ValidationFailure("FirstRuleSetFirstProp", "First RuleSet First Prop Failed"),
            new ValidationFailure("FirstRuleSetSecondProp", "First RuleSet Second Prop Failed"),
        ]);

        _secondRuleSetValidationResult = new ValidationResult(
        [
            new ValidationFailure("SecondRuleSetFirstProp", "Second RuleSet First Prop Failed"),
            new ValidationFailure("SecondRuleSetSecondProp", "Second RuleSet Second Prop Failed"),
        ]);

        _firstRuleSetAsValidator.Validate(Arg.Any<IValidationContext>()).Returns(_firstRuleSetValidationResult);
        _secondRuleSetAsValidator.Validate(Arg.Any<IValidationContext>()).Returns(_secondRuleSetValidationResult);
    }
}
