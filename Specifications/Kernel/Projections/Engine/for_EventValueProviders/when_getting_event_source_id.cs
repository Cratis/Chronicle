// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.for_EventValueProviders;

public class when_getting_event_source_id : Specification
{
    static EventSourceId eventSourceId = "2f005aaf-2f4e-4a47-92ea-63687ef74bd4";

    ValueProvider<AppendedEvent> value_provider;
    AppendedEvent @event;
    object result;

    void Establish()
    {
        @event = new(new(1, new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)), new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", DateTimeOffset.UtcNow), new JsonObject());
        value_provider = EventValueProviders.FromEventSourceId;
    }

    void Because() => result = value_provider(@event).ToString();

    [Fact] void should_return_the_event_source_id() => result.ShouldEqual(eventSourceId.ToString());
}
