// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_variant;

/// <summary>
/// A shared handler must be applicable to every variant of its identity. Skipping the variants that cannot take
/// the mapping would leave a projection quietly not maintaining a member the author declared globally, so the
/// mismatch is reported rather than absorbed.
/// </summary>
public class and_a_shared_handler_maps_a_member_the_variant_lacks : given.a_model_bound_projection_builder_for_variants
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(() => Task.FromResult(
        builder.BuildVariant(typeof(DevelopmentItem), [typeof(BacklogItem)], [typeof(WorkItemSharedHandlers)])));

    [Fact] void should_fail() => _result.ShouldNotBeNull();

    [Fact] void should_fail_with_the_inapplicable_shared_mapping_reason() => _result.ShouldBeOfExactType<GlobalHandlerPropertyNotOnVariant>();
}
