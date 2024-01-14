// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Projections.for_Projection;

public class when_next_event_is_not_of_interest : given.a_projection
{
    bool observed;
    AppendedEvent @event;
    Changeset<AppendedEvent, ExpandoObject> changeset;
    Mock<IObjectComparer> objects_comparer;

    void Establish()
    {
        var eventType = new EventType("cb1f33dd-8725-4bd2-a1a1-f372d352a7c6", 1);
        projection.SetEventTypesWithKeyResolvers(
            new EventTypeWithKeyResolver[]
            {
                    new EventTypeWithKeyResolver(eventType, KeyResolvers.FromEventSourceId)
            },
            new[] { eventType });

        @event = new(
            new(0,
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", Enumerable.Empty<Causation>(), Identity.System),
            new ExpandoObject());

        objects_comparer = new();
        objects_comparer.Setup(_ => _.Equals(IsAny<ExpandoObject>(), IsAny<AppendedEvent>(), out Ref<IEnumerable<PropertyDifference>>.IsAny)).Returns(true);

        changeset = new(objects_comparer.Object, @event, new());
        projection.Event.Subscribe(_ => observed = true);
    }

    void Because() => projection.OnNext(new(new(@event.Context.EventSourceId, ArrayIndexers.NoIndexers), @event, changeset));

    [Fact] void should_not_be_observed() => observed.ShouldBeFalse();
}
