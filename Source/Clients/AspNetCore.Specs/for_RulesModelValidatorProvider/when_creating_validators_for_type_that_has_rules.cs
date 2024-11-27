// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Rules;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Chronicle.AspNetCore.Rules.for_RulesModelValidatorProvider;

public class when_creating_validators_for_type_that_has_rules : given.one_rule_for_type
{
    ModelValidatorProviderContext _context;
    ModelMetadata _modelMetadata;

    IRule _firstRule;
    IRule _secondRule;

    void Establish()
    {
        _rules.HasFor(typeof(Model)).Returns(true);
        _rules.GetFor(typeof(Model)).Returns(
        [
            typeof(FirstRule),
            typeof(SecondRule)
        ]);
        _modelMetadata = Substitute.For<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(Model)));
        _context = new(_modelMetadata, []);

        _firstRule = new FirstRule();
        _serviceProvider.GetService(typeof(FirstRule)).Returns(_firstRule);

        _secondRule = new SecondRule();
        _serviceProvider.GetService(typeof(SecondRule)).Returns(_secondRule);
    }

    void Because() => _provider.CreateValidators(_context);

    [Fact] void should_add_validator() => _context.Results.Count.ShouldEqual(1);
    [Fact] void should_hold_both_rules_in_validator() => ((RuleModelValidator)_context.Results[0].Validator).RuleSets.ShouldContainOnly(_firstRule, _secondRule);
}
