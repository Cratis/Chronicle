// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_ordering;

public class and_ordering_by_a_property : given.a_children_builder
{
    void Because()
    {
        _builder.OrderBy(_ => _.Name);
        _result = _builder.Build();
    }

    [Fact] void should_keep_the_children_sorted() => _result.Ordering!.Placement.ShouldEqual(ChildrenPlacement.Sorted);
    [Fact] void should_sort_by_the_property() => _result.Ordering!.SortBy.ShouldEqual("Name");
    [Fact] void should_sort_ascending() => _result.Ordering!.Direction.ShouldEqual(SortDirection.Ascending);
}
