// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

public class and_a_child_only_root_loses_one_child : given.a_projection_grain_with_a_children_projection
{
    SingleReadModelProjectionState _result;

    async Task Because() => _result = await _grain.FoldForSingleReadModel(
        EventStoreNamespaceName.Default,
        SingleReadModelProjectionState.Empty,
        [
            Associated("issue", 1, "remaining-comment", "100"),
            Associated("issue", 2, "removed-comment", "200"),
            Removed("issue", 3, "removed-comment")
        ]);

    [Fact] void should_keep_the_remaining_child() => AssociationsOf(_result.Model).Select(_ => _["id"]).ShouldContainOnly("remaining-comment");
    [Fact] void should_initialize_the_root() => _result.IsInitialized.ShouldBeTrue();
}
