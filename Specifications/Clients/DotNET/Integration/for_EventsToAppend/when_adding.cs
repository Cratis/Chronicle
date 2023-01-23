// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration.for_EventsToAppend;

public class when_adding : Specification
{
    EventsToAppend events;
    string @event;

    void Establish()
    {
        events = new();
        @event = "Forty Two";
    }

    void Because() => events.Add(@event);

    [Fact] void should_hold_the_added_event() => events.First().Event.ShouldEqual(@event);
}
