// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Aksio.Cratis.Rules.for_Rules.for_RulesModelValidator;

public class when_validating_two_rule_sets_with_model_record_having_specific_key : given.two_rule_sets
{
    const string key = "a2b5bd3b-bb16-428f-b9cb-2c27b337ceb7";
    ModelValidationContext context;
    Mock<ModelMetadata> model_metadata;
    Mock<IModelMetadataProvider> model_metadata_provider;
    ModelWithKey model;
    IEnumerable<ModelValidationResult> result;

    void Establish()
    {
        model_metadata_provider = new();
        model_metadata = new(ModelMetadataIdentity.ForType(typeof(ModelWithKey)));
        model = new ModelWithKey(key);
        context = new(
            new(),
            model_metadata.Object,
            model_metadata_provider.Object,
            new object(),
            model);
    }

    void Because() => result = validator.Validate(context);

    [Fact] void should_project_to_first_rule_set_without_model_key() => rules.Verify(_ => _.ProjectTo(first_rule_set.Object, key), Once);
    [Fact] void should_project_to_second_rule_set_without_model_key() => rules.Verify(_ => _.ProjectTo(second_rule_set.Object, key), Once);
}
