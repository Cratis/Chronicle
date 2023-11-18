// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Suggestions;
using Aksio.Cratis.Kernel.Observation.Replaying;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents an implementation of <see cref="IReplayEvaluator"/>.
/// </summary>
public class ReplayEvaluator : IReplayEvaluator
{
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayEvaluator"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public ReplayEvaluator(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public async Task<bool> Evaluate(ReplayEvaluationContext context)
    {
        if (NeedsToReplay(context))
        {
            var suggestionsManager = _grainFactory.GetGrain<ISuggestionsManager>(0);
            await suggestionsManager.Add<IReplayCandidateSuggestion, ReplayCandidateRequest>(new ReplayCandidateRequest
            {
                ObserverId = context.Id,
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
