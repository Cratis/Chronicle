// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_joining;

public class and_using_a_join_builder_callback : events_with_derivatives
{
    ChildrenBuilder<Container, Item> _builder;
    JoinDefinition _result;

    void Establish()
    {
        _builder = new(new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions(), AutoMap.Inherit);
        _builder.IdentifiedBy(_ => _.Id);
    }

    void Because()
    {
        IChildrenBuilder<Container, Item> children = _builder;
#pragma warning disable IDE0039 // Verifies compatibility with an existing Action<IJoinBuilder<...>> variable
        Action<IJoinBuilder<Item, ItemChanged>> callback = join => join.Set(_ => _.Name).To(_ => _.Name);
#pragma warning restore IDE0039 // Verifies compatibility with an existing Action<IJoinBuilder<...>> variable
        children.Join<ItemChanged>(callback);
        _result = _builder.Build().Join.Single(_ => _.Key.ToClient() == typeof(ItemChanged).GetEventType()).Value;
    }

    [Fact] void should_join_on_the_child_identity() => _result.On.ShouldEqual(nameof(Item.Id));
    [Fact] void should_preserve_the_property_mapping() => _result.Properties[nameof(Item.Name)].ShouldEqual(nameof(ItemChanged.Name));
}
