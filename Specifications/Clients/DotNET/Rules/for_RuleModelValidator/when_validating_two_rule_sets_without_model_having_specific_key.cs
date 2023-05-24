// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Aksio.Cratis.Rules.for_Rules.for_RulesModelValidator;

public class when_validating_two_rule_sets_without_model_having_specific_key : given.two_rule_sets
{
    ModelValidationContext context;
    Mock<ModelMetadata> model_metadata;
    Mock<IModelMetadataProvider> model_metadata_provider;
    Model model;
    IEnumerable<ModelValidationResult> result;

    void Establish()
    {
        model_metadata_provider = new();
        model_metadata = new(ModelMetadataIdentity.ForType(typeof(Model)));
        model = new Model();
        context = new(
            new(),
            model_metadata.Object,
            model_metadata_provider.Object,
            new object(),
            model);
    }

    void Because() => result = validator.Validate(context);

    [Fact] void should_project_to_first_rule_set_without_model_key() => rules.Verify(_ => _.ProjectTo(first_rule_set.Object, null), Once);
    [Fact] void should_project_to_second_rule_set_without_model_key() => rules.Verify(_ => _.ProjectTo(second_rule_set.Object, null), Once);
    [Fact] void should_add_first_rule_first_failure() => result.ToArray()[0].MemberName.ShouldEqual(first_rule_set_validation_result.Errors.ToArray()[0].PropertyName);
    [Fact] void should_add_first_rule_second_failure() => result.ToArray()[1].MemberName.ShouldEqual(first_rule_set_validation_result.Errors.ToArray()[1].PropertyName);
    [Fact] void should_add_second_rule_first_failure() => result.ToArray()[2].MemberName.ShouldEqual(second_rule_set_validation_result.Errors.ToArray()[0].PropertyName);
    [Fact] void should_add_second_rule_second_failure() => result.ToArray()[3].MemberName.ShouldEqual(second_rule_set_validation_result.Errors.ToArray()[1].PropertyName);
}
