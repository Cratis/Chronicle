// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.given;

public class a_reactor_builder : Specification
{
    protected EventTypesForSpecifications _eventTypes;
    internal ReactorBuilder _builder;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(OrderPlaced), typeof(OrderShipped), typeof(CustomerRegistered)]);
        _builder = new ReactorBuilder(_eventTypes);
    }

    [EventType]
    public record OrderPlaced(string OrderNumber);

    [EventType]
    public record OrderShipped(string OrderNumber);

    [EventType]
    public record CustomerRegistered(string Name);

    public record NotAnEvent(string Name);
}
