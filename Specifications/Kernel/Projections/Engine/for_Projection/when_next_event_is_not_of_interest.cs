// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.for_Projection;

public class when_next_event_is_not_of_interest : given.a_projection
{
    bool observed;
    AppendedEvent @event;
    Changeset<AppendedEvent, ExpandoObject> changeset;
    Mock<IObjectsComparer> objects_comparer;

    void Establish()
    {
        projection.SetEventTypesWithKeyResolvers(new EventTypeWithKeyResolver[]
        {
                new EventTypeWithKeyResolver(new  EventType("aac3d310-ff2f-4809-a326-afe14dd9a3d6", 1), KeyResolvers.FromEventSourceId)
        });

        @event = new(
            new(0,
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", "50308963-d8b5-4b6e-97c7-e2486e8237e1", "bfb7fd4a-1822-4937-a6d1-52464a173f84"),
            new JsonObject());

        objects_comparer = new();
        objects_comparer.Setup(_ => _.Equals(IsAny<ExpandoObject>(), IsAny<AppendedEvent>(), out Ref<IEnumerable<PropertyDifference>>.IsAny)).Returns(true);

        changeset = new(objects_comparer.Object, @event, new());
        projection.Event.Subscribe(_ => observed = true);
    }

    void Because() => projection.OnNext(new(new(@event.Context.EventSourceId, ArrayIndexers.NoIndexers), @event, changeset));

    [Fact] void should_not_be_observed() => observed.ShouldBeFalse();
}
