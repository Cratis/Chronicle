// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections
{
    public class when_accessing_event_source_id : Specification
    {
        Event @event;
        object result;

        void Establish() => @event = new Event(0, new("efd7d03b-21f3-4f09-99f3-355779eb5a11", 1), DateTimeOffset.UtcNow, "463f43c9-16b7-4fd9-9f37-d4d340d89ce8", new ExpandoObject());

        void Because() => result = EventValueProviders.FromEventSourceId(@event);

        [Fact] void should_return_the_guid_from_event_source_id_from_the_event() => result.ShouldEqual(@event.EventSourceId.Value);
    }
}
