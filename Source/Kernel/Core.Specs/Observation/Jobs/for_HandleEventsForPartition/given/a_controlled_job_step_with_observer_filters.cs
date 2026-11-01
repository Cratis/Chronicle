// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.given;

public class a_controlled_job_step_with_observer_filters : a_performing_job_step_with_observer_filters
{
    protected static readonly EventType _a = new("529bd8fb-d0ce-4d40-992b-205ee551f217", 1);
    protected static readonly EventType _e = new("758264df-adc9-4745-9b3c-85ea6920b2df", 1);

    protected static AppendedEvent CreateTypedEvent(EventType type, ulong sequenceNumber, bool included = true)
    {
        var @event = CreateEvent(sequenceNumber, included ? _audited : []);
        return @event with { Context = @event.Context with { EventType = type } };
    }

    void Establish()
    {
        _observer.GetSubscription().Returns(new ObserverSubscription(
            "observer-id",
            new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log),
            [_a, _e],
            typeof(ISomeObserverType),
            SiloAddress.Zero,
            Filters: Filters)
        {
            TypeSetRevision = new(2),
            TypeSetMembers = new Dictionary<EventTypeId, ObserverTypeSetMember>
            {
                [_a.Id] = new(new(1), null),
                [_e.Id] = new(new(2), 10)
            },
            Targets = [new(SiloAddress.Zero) { DeclaredEventTypes = [_a], GenerationDelivery = EventGenerationDelivery.Compatibility }]
        });
    }
}
