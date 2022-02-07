// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;
using NJsonSchema;

namespace Aksio.Cratis.Events.Projections.for_Projection
{
    public class when_next_event_is_not_of_interest : Specification
    {
        Projection projection;
        bool observed;
        AppendedEvent @event;
        Changeset<AppendedEvent, ExpandoObject> changeset;

        void Establish()
        {
            projection = new Projection(
                "0b7325dd-7a25-4681-9ab7-c387a6073547",
                string.Empty,
                string.Empty,
                string.Empty,
                new Model(string.Empty, new JsonSchema()),
                false,
                true,
                Array.Empty<IProjection>());

            @event = new(new(0, new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)), new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", DateTimeOffset.UtcNow), new JsonObject());

            changeset = new(@event, new());
            projection.Event.Subscribe(_ => observed = true);
        }

        void Because() => projection.OnNext(new(new(@event.Context.EventSourceId, ArrayIndexer.NoIndexers), @event, changeset));

        [Fact] void should_not_be_observed() => observed.ShouldBeFalse();
    }
}
