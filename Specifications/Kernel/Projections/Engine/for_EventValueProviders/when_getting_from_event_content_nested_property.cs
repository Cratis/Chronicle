// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.for_EventValueProviders
{
    public class when_getting_from_event_content_nested_property : Specification
    {
        ValueProvider<Event> value_provider;
        Event @event;
        object result;

        void Establish()
        {
            dynamic content = new ExpandoObject();
            content.nested = new ExpandoObject();
            content.nested.sourceString = "Forty two";
            @event = new Event(0, new  EventType("02405794-91e7-4e4f-8ad1-f043070ca297", 1), DateTimeOffset.UtcNow, "2f005aaf-2f4e-4a47-92ea-63687ef74bd4", content);
            value_provider = EventValueProviders.FromEventContent("Nested.SourceString");
        }

        void Because() => result = value_provider(@event);

        [Fact] void should_return_content_of_source_property() => result.ShouldEqual("Forty two");
    }
}
