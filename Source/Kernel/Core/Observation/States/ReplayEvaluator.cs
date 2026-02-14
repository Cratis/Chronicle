// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Grains.Recommendations;

namespace Cratis.Chronicle.Observation.States;

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
        if (!CanReplay(context))
        {
            return false;
        }

        var recommendationsManager = grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(eventStore, @namespace));
        await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            "Event types has changed.",
            new()
            {
                ObserverId = context.Id,
                ObserverKey = context.Key,
                Reasons =
                [
                    new EventTypesChangedReplayCandidateReason(
                        context.Definition.EventTypes,
                        context.Subscription.EventTypes)
                ]
            });

        return false;
    }

    static bool CanReplay(ReplayEvaluationContext context) =>
        context.Definition.IsReplayable && NeedsToReplay(context);

    static bool NeedsToReplay(ReplayEvaluationContext context) =>
        HasDefinitionChanged(context) && HasEventsInSequence(context);

    static bool HasEventsInSequence(ReplayEvaluationContext context) =>
        context.TailEventSequenceNumber.IsActualValue && context.TailEventSequenceNumberForEventTypes.IsActualValue;

    static bool HasDefinitionChanged(ReplayEvaluationContext context) =>
            context.Definition.EventTypes.Count() != context.Subscription.EventTypes.Count() ||
            !context.Subscription.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(context.Definition.EventTypes.OrderBy(_ => _.Id.Value));
}
