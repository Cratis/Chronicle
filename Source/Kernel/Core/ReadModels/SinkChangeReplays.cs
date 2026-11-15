// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="ISinkChangeReplays"/>.
/// </summary>
/// <remarks>
/// A read model moved to another sink finds its new store empty while its observers are already caught up, so it is rebuilt the
/// way a changed projection or reducer definition is: a full replay when the definition evolution policy allows one, and a
/// replay recommendation when it does not or the observer refuses it - for a global projection, say, or during generation
/// initialization. The replays go through replay recommendations, which are durable and recorded only once per observer.
/// </remarks>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="storage">The <see cref="IStorage"/> holding the reducer definitions.</param>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the definition evolution policy.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class SinkChangeReplays(IGrainFactory grainFactory, IStorage storage, IOptions<ChronicleOptions> options, ILogger<SinkChangeReplays> logger) : ISinkChangeReplays
{
    const string Description = "Read model sink has changed.";

    /// <inheritdoc/>
    public async Task<SinkChangeReplay> Request(EventStoreName eventStore, ReadModelIdentifier readModel)
    {
        var projections = (await grainFactory.GetGrain<IProjectionsManager>(eventStore).GetProjectionDefinitions())
            .Where(_ => _.ReadModel == readModel)
            .Select(_ => new Target(_.Identifier.Value, _.EventSequenceId, ObserverType.Projection));
        var reducers = (await storage.GetEventStore(eventStore).Reducers.GetAll())
            .Where(_ => _.ReadModel == readModel)
            .Select(_ => new Target(_.Identifier.Value, _.EventSequenceId, ObserverType.Reducer));
        var targets = projections.Concat(reducers).ToArray();
        var recommendations = new List<(EventStoreNamespaceName Namespace, RecommendationId Recommendation)>();
        if (targets.Length == 0)
        {
            return new(eventStore, readModel, recommendations);
        }

        foreach (var @namespace in await grainFactory.GetGrain<INamespaces>(eventStore).GetAll())
        {
            foreach (var target in targets)
            {
                var key = new ObserverKey(target.Observer, eventStore, @namespace, target.EventSequence);
                recommendations.Add((@namespace, await Recommend(key, target)));
            }
        }

        return new(eventStore, readModel, recommendations);
    }

    /// <inheritdoc/>
    public async Task Start(SinkChangeReplay replay)
    {
        if (!options.Value.Observers.DefinitionEvolution.Allows(DefinitionEvolutionOperation.FullReplay))
        {
            return;
        }

        foreach (var (@namespace, recommendation) in replay.Recommendations)
        {
            try
            {
                logger.ReplayingIntoChangedSink(replay.ReadModel, @namespace);
                await grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(replay.EventStore, @namespace)).Perform(recommendation);
            }
#pragma warning disable CA1031 // A refused replay stays recommended, as a refused definition-change replay does.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.ReplayIntoChangedSinkRefused(replay.ReadModel, @namespace, exception);
            }
        }
    }

    async Task<RecommendationId> Recommend(ObserverKey key, Target target) =>
        await grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, key.Namespace))
            .Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                Description,
                new()
                {
                    ObserverId = key.ObserverId,
                    ObserverKey = key,
                    ObserverType = target.Type,
                    Reasons = [target.Type == ObserverType.Reducer ? new ReducerDefinitionChangedReplayCandidateReason() : new ProjectionDefinitionChangedReplayCandidateReason()]
                });

    sealed record Target(ObserverId Observer, EventSequenceId EventSequence, ObserverType Type);
}
