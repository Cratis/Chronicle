// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_batch_methods.given;

public class a_registry : Specification
{
    protected IEventTypes _eventTypes;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.AllClrTypes.Returns(new[] { typeof(Registered), typeof(Renamed), typeof(Removed) }.ToImmutableList());
        _eventTypes.GetEventTypeFor(typeof(Registered)).Returns(new EventType("registered", 1));
        _eventTypes.GetEventTypeFor(typeof(Renamed)).Returns(new EventType("renamed", 1));
        _eventTypes.GetEventTypeFor(typeof(Removed)).Returns(new EventType("removed", 1));
    }

    public record Registered;
    public record Renamed;
    public record Removed;
    public record NotAnEvent;
}
