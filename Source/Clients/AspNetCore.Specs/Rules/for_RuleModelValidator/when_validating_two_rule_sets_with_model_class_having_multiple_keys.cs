// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Rules;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Chronicle.AspNetCore.Rules.for_Rules.for_RulesModelValidator;

public class when_validating_two_rule_sets_with_model_class_having_multiple_keys : given.two_rule_sets
{
    const string key = "a2b5bd3b-bb16-428f-b9cb-2c27b337ceb7";
    ModelValidationContext _context;
    ModelMetadata _modelMetadata;
    IModelMetadataProvider _modelMetadataProvider;
    ModelClassWithMultipleKeys _model;
    Exception _result;

    void Establish()
    {
        _modelMetadataProvider = Substitute.For<IModelMetadataProvider>();
        _modelMetadata = Substitute.For<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(ModelClassWithMultipleKeys)));
        _model = new ModelClassWithMultipleKeys
        {
            Id = key,
            SecondId = key
        };
        _context = new(
            new(),
            _modelMetadata,
            _modelMetadataProvider,
            new object(),
            _model);
    }

    void Because() => _result = Catch.Exception(() => _validator.Validate(_context));

    [Fact] void should_throw_invalid_number_of_model_keys() => _result.ShouldBeOfExactType<InvalidNumberOfModelKeys>();
}
