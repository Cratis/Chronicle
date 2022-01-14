// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Properties;

namespace Cratis.Events.Projections.for_EventValueProviders
{
    public class when_getting_event_source_id : Specification
    {
        static EventSourceId    eventSourceId = "2f005aaf-2f4e-4a47-92ea-63687ef74bd4";

        ValueProvider<Event> value_provider;
        Event @event;
        object result;

        void Establish()
        {
            dynamic content = new ExpandoObject();
            @event = new Event(0, new  EventType("02405794-91e7-4e4f-8ad1-f043070ca297", 1), DateTimeOffset.UtcNow, eventSourceId, content);
            value_provider = EventValueProviders.FromEventSourceId;
        }

        void Because() => result = value_provider(@event).ToString();

        [Fact] void should_return_the_event_source_id() => result.ShouldEqual(eventSourceId.ToString());
    }
}
