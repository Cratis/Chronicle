// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenJoinBuilder.when_building;

public class and_identity_is_configured : events_with_derivatives
{
    ChildrenJoinBuilder<Container, Item, ItemChanged> _builder;
    JoinDefinition _result;

    void Establish()
    {
        var children = new ChildrenBuilder<Container, Item>(new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions(), AutoMap.Inherit);
        children.IdentifiedBy(_ => _.Id);
        _builder = new(children, new DefaultNamingPolicy());
        _builder.Set(_ => _.Name).To(_ => _.Name);
        _builder.UsingKey(_ => _.Id);
    }

    void Because() => _result = _builder.Build();

    [Fact] void should_join_on_the_child_identity() => _result.On.ShouldEqual(nameof(Item.Id));
    [Fact] void should_preserve_the_property_mapping() => _result.Properties[nameof(Item.Name)].ShouldEqual(nameof(ItemChanged.Name));
    [Fact] void should_preserve_the_key() => _result.Key.ShouldEqual(nameof(ItemChanged.Id));
}
