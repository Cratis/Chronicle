// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_ordering;

public class and_ordering_descending_by_a_property_with_camel_case_naming : given.a_children_builder
{
    protected override INamingPolicy NamingPolicy => new CamelCaseNamingPolicy();

    void Because()
    {
        _builder.OrderByDescending(_ => _.Added);
        _result = _builder.Build();
    }

    [Fact] void should_keep_the_children_sorted() => _result.Ordering!.Placement.ShouldEqual(ChildrenPlacement.Sorted);
    [Fact] void should_sort_by_the_property_named_by_the_policy() => _result.Ordering!.SortBy.ShouldEqual("added");
    [Fact] void should_sort_descending() => _result.Ordering!.Direction.ShouldEqual(SortDirection.Descending);
}
