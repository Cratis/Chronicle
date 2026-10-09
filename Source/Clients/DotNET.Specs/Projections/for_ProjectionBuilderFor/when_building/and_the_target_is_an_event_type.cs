// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_the_target_is_an_event_type : Specification
{
    [EventType]
    public record OrderPacked(string Carrier);

    [EventType, Public]
    public record OrderShipped(string Carrier);

    IEventTypes _eventTypes;
    ProjectionBuilderFor<OrderShipped> _builder;
    ProjectionDefinition _result;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(OrderPacked), typeof(OrderShipped)]);
        _builder = new("order-shipped-publisher", typeof(OrderShipped), new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions());
    }

    void Because()
    {
        _builder.From<OrderPacked>(from => from.Set(_ => _.Carrier).To(_ => _.Carrier));
        _result = _builder.Build();
    }

    [Fact] void should_identify_the_target_by_the_event_type_clr_name() => _result.ReadModel.ShouldEqual(typeof(OrderShipped).FullName);
    [Fact] void should_consume_the_private_event() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(OrderPacked).GetEventType());
    [Fact] void should_map_the_property() => _result.From.Single().Value.Properties[nameof(OrderShipped.Carrier)].ShouldEqual(nameof(OrderPacked.Carrier));
}
