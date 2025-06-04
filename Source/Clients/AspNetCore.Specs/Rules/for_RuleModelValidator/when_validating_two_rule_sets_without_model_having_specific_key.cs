// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Chronicle.AspNetCore.Rules.for_Rules.for_RulesModelValidator;

public class when_validating_two_rule_sets_without_model_having_specific_key : given.two_rule_sets
{
    ModelValidationContext _context;
    ModelMetadata _modelMetadata;
    IModelMetadataProvider _modelMetadataProvider;
    Model _model;
    IEnumerable<ModelValidationResult> _result;

    void Establish()
    {
        _modelMetadataProvider = Substitute.For<IModelMetadataProvider>();
        _modelMetadata = Substitute.For<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(Model)));
        _model = new Model();
        _context = new(
            new(),
            _modelMetadata,
            _modelMetadataProvider,
            new object(),
            _model);
    }

    void Because() => _result = _validator.Validate(_context);

    [Fact] void should_project_to_first_rule_set_without_model_key() => _rules.Received(1).ProjectTo(_firstRuleSet, null);
    [Fact] void should_project_to_second_rule_set_without_model_key() => _rules.Received(1).ProjectTo(_secondRuleSet, null);
    [Fact] void should_add_first_rule_first_failure() => _result.ToArray()[0].MemberName.ShouldEqual(_firstRuleSetValidationResult.Errors.ToArray()[0].PropertyName);
    [Fact] void should_add_first_rule_second_failure() => _result.ToArray()[1].MemberName.ShouldEqual(_firstRuleSetValidationResult.Errors.ToArray()[1].PropertyName);
    [Fact] void should_add_second_rule_first_failure() => _result.ToArray()[2].MemberName.ShouldEqual(_secondRuleSetValidationResult.Errors.ToArray()[0].PropertyName);
    [Fact] void should_add_second_rule_second_failure() => _result.ToArray()[3].MemberName.ShouldEqual(_secondRuleSetValidationResult.Errors.ToArray()[1].PropertyName);
}
