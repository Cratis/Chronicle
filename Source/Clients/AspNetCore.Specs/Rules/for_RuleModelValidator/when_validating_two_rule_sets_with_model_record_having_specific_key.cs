// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Chronicle.AspNetCore.Rules.for_Rules.for_RulesModelValidator;

public class when_validating_two_rule_sets_with_model_record_having_specific_key : given.two_rule_sets
{
    const string key = "a2b5bd3b-bb16-428f-b9cb-2c27b337ceb7";
    ModelValidationContext _context;
    ModelMetadata _modelMetadata;
    IModelMetadataProvider _modelMetadataProvider;
    ModelWithKey _model;
    IEnumerable<ModelValidationResult> _result;

    void Establish()
    {
        _modelMetadataProvider = Substitute.For<IModelMetadataProvider>();
        _modelMetadata = Substitute.For<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(ModelWithKey)));
        _model = new ModelWithKey(key);
        _context = new(
            new(),
            _modelMetadata,
            _modelMetadataProvider,
            new object(),
            _model);
    }

    void Because() => _result = _validator.Validate(_context);

    [Fact] void should_project_to_first_rule_set_without_model_key() => _rules.Received(1).ProjectTo(_firstRuleSet, key);
    [Fact] void should_project_to_second_rule_set_without_model_key() => _rules.Received(1).ProjectTo(_secondRuleSet, key);
}
