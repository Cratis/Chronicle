// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenJoinBuilder.when_building;

public class and_on_is_specified : events_with_derivatives
{
    ChildrenJoinBuilder<Container, Item, ItemChanged> _builder;
    Exception _error;

    void Establish()
    {
        var children = new ChildrenBuilder<Container, Item>(new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions(), AutoMap.Inherit);
        children.IdentifiedBy(_ => _.Id);
        _builder = new(children, new DefaultNamingPolicy());
#pragma warning disable CS0618 // Verifies the deprecated On() keeps its runtime behavior
        _builder.On(_ => _.Id);
#pragma warning restore CS0618 // Verifies the deprecated On() keeps its runtime behavior
    }

    void Because() => _error = Catch.Exception(() => _builder.Build());

    [Fact] void should_reject_the_explicit_join_property() => _error.ShouldBeOfExactType<OnPropertyShouldNotBeSpecifiedForChildJoin>();
}
