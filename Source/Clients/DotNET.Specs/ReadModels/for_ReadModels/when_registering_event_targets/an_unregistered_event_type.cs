// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class an_unregistered_event_type : given.an_event_target<an_unregistered_event_type.OrderShipped>
{
    [EventType, Public]
    public record OrderShipped(string Id);

    void Establish() => _eventTypes.HasFor(Arg.Any<EventTypeId>()).Returns(false);

    async Task Because() => await Exercise();

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<TypeIsNotAnEventType>();
}
