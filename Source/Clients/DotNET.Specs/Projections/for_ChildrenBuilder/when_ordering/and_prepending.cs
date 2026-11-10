// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_ordering;

public class and_prepending : given.a_children_builder
{
    void Because()
    {
        _builder.Prepend();
        _result = _builder.Build();
    }

    [Fact] void should_place_new_children_first() => _result.Ordering!.Placement.ShouldEqual(ChildrenPlacement.Prepend);
    [Fact] void should_not_sort_by_anything() => _result.Ordering!.SortBy.ShouldEqual(string.Empty);
}
