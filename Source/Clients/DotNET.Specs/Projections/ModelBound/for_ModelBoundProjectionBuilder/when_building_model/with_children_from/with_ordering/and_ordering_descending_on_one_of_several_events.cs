// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from.with_ordering;

/// <summary>
/// Ordering belongs to the collection, so declaring it on one of the attributes orders children added by every event.
/// </summary>
public class and_ordering_descending_on_one_of_several_events : given.a_model_bound_projection_builder_for_ordered_children
{
    ChildrenDefinition _children;

    void Because() => _children = builder.Build(typeof(given.EntriesOrderedByRankDescending)).Children[nameof(given.EntriesOrderedByRankDescending.Entries)];

    [Fact] void should_project_from_both_events() => _children.From.Count.ShouldEqual(2);
    [Fact] void should_keep_the_children_sorted() => _children.Ordering!.Placement.ShouldEqual(ChildrenPlacement.Sorted);
    [Fact] void should_sort_by_the_property() => _children.Ordering!.SortBy.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(given.Entry.Rank))));
    [Fact] void should_sort_descending() => _children.Ordering!.Direction.ShouldEqual(SortDirection.Descending);
}
