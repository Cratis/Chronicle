// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from.with_ordering;

public class and_ordering_by_a_property : given.a_model_bound_projection_builder_for_ordered_children
{
    ChildrenOrderingDefinition _ordering;

    void Because() => _ordering = builder.Build(typeof(given.EntriesOrderedByName)).Children[nameof(given.EntriesOrderedByName.Entries)].Ordering!;

    [Fact] void should_keep_the_children_sorted() => _ordering.Placement.ShouldEqual(ChildrenPlacement.Sorted);
    [Fact] void should_sort_by_the_property() => _ordering.SortBy.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(given.Entry.Name))));
    [Fact] void should_sort_ascending() => _ordering.Direction.ShouldEqual(SortDirection.Ascending);
}
