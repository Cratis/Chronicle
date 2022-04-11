// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.given;

public class a_connected_observer_and_two_event_types_and_one_event_in_sequence : a_connected_observer_and_two_event_types
{
    protected EventSourceId event_source_id;
    protected Mock<IPartitionedObserver> partitioned_observer;
    protected AppendedEvent appended_event;

    void Establish()
    {
        event_source_id = Guid.NewGuid().ToString();
        partitioned_observer = new();
        var key = new PartitionedObserverKey(microservice_id, tenant_id, event_sequence_id, event_source_id);
        grain_factory.Setup(_ => _.GetGrain<IPartitionedObserver>(observer_id, key, null)).Returns(partitioned_observer.Object);

        appended_event = new AppendedEvent(
            new(EventSequenceNumber.First, event_types.ToArray()[0]),
            new(event_source_id, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new JsonObject());

        var subscription = new Mock<StreamSubscriptionHandle<AppendedEvent>>();
        event_sequence.Setup(_ => _.GetNextSequenceNumber()).Returns(Task.FromResult((EventSequenceNumber)1));
        stream.Setup(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()))
            .Returns((IAsyncObserver<AppendedEvent> observer, StreamSequenceToken _, StreamFilterPredicate __, object ___) =>
            {
                observer.OnNextAsync(appended_event);

                return Task.FromResult(subscription.Object);
            });

        storage.Invocations.Clear();
    }
}
