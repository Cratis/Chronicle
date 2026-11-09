// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.with_ordered_children;

public class and_the_sort_property_is_not_on_the_child : given.a_projection_factory_for_definitions
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(() => CreateWithOrderedChildren(new(ChildrenPlacement.Sorted, new("missing"))));

    [Fact] void should_throw_invalid_children_ordering() => _result.ShouldBeOfExactType<InvalidChildrenOrdering>();
    [Fact] void should_name_the_sort_property() => _result.Message.ShouldContain("'missing'");
}
