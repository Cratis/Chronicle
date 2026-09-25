// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_variant;

public class and_the_variant_declares_no_entering_event : given.a_model_bound_projection_builder_for_variants
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(() => Task.FromResult(builder.BuildVariant(typeof(MissingEntersOnVariant), [])));

    [Fact] void should_fail() => _result.ShouldNotBeNull();

    [Fact] void should_fail_with_the_missing_enters_on_reason() => _result.ShouldBeOfExactType<VariantMustDeclareEntersOnEvent>();
}
