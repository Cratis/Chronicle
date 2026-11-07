// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_JoinBuilder.when_building;

public class and_on_is_specified_through_the_inherited_child_join : events_with_derivatives
{
    IProjectionBuilder<Item, IChildrenBuilder<Container, Item>> _children;
    Exception _error;

    void Establish()
    {
        var children = new ChildrenBuilder<Container, Item>(new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions(), AutoMap.Inherit);
        _children = children.IdentifiedBy(_ => _.Id);
    }

    void Because() => _error = Catch.Exception(() => _children.Join<ItemChanged>(join => join.On(_ => _.Id)));

    [Fact] void should_reject_the_explicit_join_property() => _error.ShouldBeOfExactType<OnPropertyShouldNotBeSpecifiedForChildJoin>();
}
