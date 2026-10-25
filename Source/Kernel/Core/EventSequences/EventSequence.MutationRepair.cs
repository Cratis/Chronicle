// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;
using IObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    async Task ReplayForContentMutation(EventSourceId eventSourceId, IEnumerable<EventType> affectedEventTypes)
    {
        var observers = (await ObserverStorage.GetReplayableObserversForEventTypes(affectedEventTypes))
            .DistinctBy(observer => observer.Identifier)
            .ToArray();
        var projectionDefinitions = await GetProjectionDefinitionsForRepair(observers);
        foreach (var observer in observers)
        {
            var key = new ObserverKey(observer.Identifier, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId);
            var grain = GrainFactory.GetGrain<IObserver>(key);
            if (observer.Type == ObserverType.Projection)
            {
                projectionDefinitions.TryGetValue(observer.Identifier.Value, out var definition);
                if (PartitionReplayabilityClassifier.Classify(observer.Type, definition) == PartitionReplayability.Open)
                {
                    await grain.Replay();
                    continue;
                }
            }

            await grain.ReplayPartition(eventSourceId);
        }
    }

    async Task<IReadOnlyDictionary<string, ProjectionDefinition>> GetProjectionDefinitionsForRepair(IEnumerable<ObserverDefinition> observers)
    {
        if (!observers.Any(observer => observer.Type == ObserverType.Projection))
        {
            return new Dictionary<string, ProjectionDefinition>();
        }

        var definitions = await storage.GetEventStore(_eventSequenceKey.EventStore).Projections.GetAll();
        return definitions.ToDictionary(definition => definition.Identifier.Value);
    }
}
