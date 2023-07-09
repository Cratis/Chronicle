// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Rules;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Aksio.Cratis.AspNetCore.Rules.for_RulesModelValidatorProvider;

public class when_creating_validators_for_type_that_has_rules : given.one_rule_for_type
{
    ModelValidatorProviderContext context;
    Mock<ModelMetadata> model_metadata;

    IRule first_rule;
    IRule second_rule;

    void Establish()
    {
        rules.Setup(_ => _.HasFor(typeof(Model))).Returns(true);
        rules.Setup(_ => _.GetFor(typeof(Model))).Returns(new[]
        {
            typeof(FirstRule),
            typeof(SecondRule)
        });
        model_metadata = new(ModelMetadataIdentity.ForType(typeof(Model)));
        context = new(model_metadata.Object, new List<ValidatorItem>());

        first_rule = new FirstRule();
        service_provider.Setup(_ => _.GetService(typeof(FirstRule))).Returns(first_rule);

        second_rule = new SecondRule();
        service_provider.Setup(_ => _.GetService(typeof(SecondRule))).Returns(second_rule);
    }

    void Because() => provider.CreateValidators(context);

    [Fact] void should_add_validator() => context.Results.Count.ShouldEqual(1);
    [Fact] void should_hold_both_rules_in_validator() => ((RuleModelValidator)context.Results[0].Validator).RuleSets.ShouldContainOnly(first_rule, second_rule);
}
