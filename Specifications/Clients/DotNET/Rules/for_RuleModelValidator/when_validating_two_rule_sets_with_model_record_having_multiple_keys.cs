// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Aksio.Cratis.Rules.for_Rules.for_RulesModelValidator;

public class when_validating_two_rule_sets_with_model_record_having_multiple_keys : given.two_rule_sets
{
    const string key = "a2b5bd3b-bb16-428f-b9cb-2c27b337ceb7";
    ModelValidationContext context;
    Mock<ModelMetadata> model_metadata;
    Mock<IModelMetadataProvider> model_metadata_provider;
    ModelWithMultipleKeys model;
    Exception result;

    void Establish()
    {
        model_metadata_provider = new();
        model_metadata = new(ModelMetadataIdentity.ForType(typeof(ModelWithMultipleKeys)));
        model = new ModelWithMultipleKeys(key, key);
        context = new(
            new(),
            model_metadata.Object,
            model_metadata_provider.Object,
            new object(),
            model);
    }

    void Because() => result = Catch.Exception(() => validator.Validate(context));

    [Fact] void should_throw_invalid_number_of_model_keys() => result.ShouldBeOfExactType<InvalidNumberOfModelKeys>();
}
