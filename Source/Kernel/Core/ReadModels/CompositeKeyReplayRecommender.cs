// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="ICompositeKeyReplayRecommender"/> that adds a replay recommendation for the observer.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for getting the recommendations manager.</param>
public class CompositeKeyReplayRecommender(IGrainFactory grainFactory) : ICompositeKeyReplayRecommender
{
    /// <inheritdoc/>
    public async Task Recommend(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
    {
        ObserverId observerId = readModel.ObserverIdentifier.Value;
        var recommendationsManager = grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(eventStore, @namespace));
        await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            $"Read model '{readModel.Identifier}' holds rows stored under the legacy composite key format that cannot be migrated in place. Replay to rewrite the keys.",
            new()
            {
                ObserverId = observerId,
                ObserverKey = new(observerId, eventStore, @namespace, EventSequenceId.Log),
                Reasons = [new ProjectionDefinitionChangedReplayCandidateReason()]
            });
    }
}
