// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from.with_ordering;

public class and_prepending : given.a_model_bound_projection_builder_for_ordered_children
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(given.PrependedEntries));

    [Fact] void should_place_new_children_first() => _result.Children[nameof(given.PrependedEntries.Entries)].Ordering!.Placement.ShouldEqual(ChildrenPlacement.Prepend);
}
