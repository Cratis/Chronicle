// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from.with_ordering;

public class and_no_ordering_is_declared : given.a_model_bound_projection_builder_for_ordered_children
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(given.AppendedEntries));

    [Fact] void should_leave_the_ordering_out() => _result.Children[nameof(given.AppendedEntries.Entries)].Ordering.ShouldBeNull();
}
