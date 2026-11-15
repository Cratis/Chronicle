// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_joining;

public class and_using_a_lambda : events_with_derivatives
{
    ChildrenBuilder<Container, Item> _builder;
    Type _callbackType;
    JoinDefinition _result;

    void Establish()
    {
        _builder = new(new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions(), AutoMap.Inherit);
        _builder.IdentifiedBy(_ => _.Id);
    }

    void Because()
    {
        IChildrenBuilder<Container, Item> children = _builder;
        children.Join<ItemChanged>(join => _callbackType = StaticTypeOf(join));
        _result = _builder.Build().Join.Single(_ => _.Key.ToClient() == typeof(ItemChanged).GetEventType()).Value;
    }

    static Type StaticTypeOf<T>(T _) => typeof(T);

    [Fact] void should_use_the_child_specific_builder() => _callbackType.ShouldEqual(typeof(IChildrenJoinBuilder<Item, ItemChanged>));
    [Fact] void should_join_on_the_child_identity() => _result.On.ShouldEqual(nameof(Item.Id));
}
