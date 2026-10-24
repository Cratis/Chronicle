// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using IObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    async Task ReplayForContentMutation(EventSourceId eventSourceId, IEnumerable<EventType> affectedEventTypes)
    {
        var observers = await ObserverStorage.GetReplayableObserversForEventTypes(affectedEventTypes);
        foreach (var observer in observers.DistinctBy(observer => observer.Identifier))
        {
            var key = new ObserverKey(observer.Identifier, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId);
            var grain = GrainFactory.GetGrain<IObserver>(key);
            if (observer.Type == ObserverType.Projection)
            {
                var definition = await storage.GetEventStore(_eventSequenceKey.EventStore).Projections.Get(observer.Identifier.Value);
                if (PartitionReplayabilityClassifier.Classify(observer.Type, definition) == PartitionReplayability.Open)
                {
                    await grain.Replay();
                    continue;
                }
            }

            await grain.ReplayPartition(eventSourceId);
        }
    }
}
