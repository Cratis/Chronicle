// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Grains.Recommendations;
using Cratis.Kernel.Observation.Replaying;

namespace Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents an implementation of <see cref="IReplayEvaluator"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReplayEvaluator"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/> the evaluator is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the evaluator is for.</param>
public class ReplayEvaluator(
    IGrainFactory grainFactory,
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace) : IReplayEvaluator
{
    /// <inheritdoc/>
    public async Task<bool> Evaluate(ReplayEvaluationContext context)
    {
        if (NeedsToReplay(context))
        {
            var recommendationsManager = grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(eventStore, @namespace));
            await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Event types has changed.",
                new ReplayCandidateRequest
                {
                    ObserverId = context.Id,
                    ObserverKey = context.Key,
                    Reasons = new[]
                    {
                        new EventTypesChangedReplayCandidateReason(
                            context.State.EventTypes,
                            context.Subscription.EventTypes)
                    }
                });
        }

        return false;
    }

    bool NeedsToReplay(ReplayEvaluationContext context) =>
        HasDefinitionChanged(context) && HasEventsInSequence(context);

    bool HasEventsInSequence(ReplayEvaluationContext context) =>
        context.TailEventSequenceNumber.IsActualValue && context.TailEventSequenceNumberForEventTypes.IsActualValue;

    bool HasDefinitionChanged(ReplayEvaluationContext context) =>
            context.State.EventTypes.Count() != context.Subscription.EventTypes.Count() ||
            !context.Subscription.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(context.State.EventTypes.OrderBy(_ => _.Id.Value));
}
