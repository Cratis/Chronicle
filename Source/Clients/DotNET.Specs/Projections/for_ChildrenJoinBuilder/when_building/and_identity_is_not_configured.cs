// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenJoinBuilder.when_building;

public class and_identity_is_not_configured : events_with_derivatives
{
    ChildrenJoinBuilder<Container, Item, ItemChanged> _builder;
    Exception _error;

    void Establish()
    {
        var children = new ChildrenBuilder<Container, Item>(new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions(), AutoMap.Inherit);
        _builder = new(children, new DefaultNamingPolicy());
    }

    void Because() => _error = Catch.Exception(() => _builder.Build());

    [Fact] void should_require_the_child_identity() => _error.ShouldBeOfExactType<MissingIdentifiedByPropertyExpressionWhenJoiningWithEvent>();
}
