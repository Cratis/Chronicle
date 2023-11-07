// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation.Replaying;
using Aksio.Cratis.Kernel.Persistence.Observation.Replaying;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents an implementation of <see cref="IReplayEvaluator"/>.
/// </summary>
public class ReplayEvaluator : IReplayEvaluator
{
    readonly IReplayCandidatesStorage _replayCandidatesStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayEvaluator"/> class.
    /// </summary>
    /// <param name="replayCandidatesStorage"><see cref="IReplayCandidatesStorage"/> for storing candidates for replaying.</param>
    public ReplayEvaluator(IReplayCandidatesStorage replayCandidatesStorage)
    {
        _replayCandidatesStorage = replayCandidatesStorage;
    }

    /// <inheritdoc/>
    public async Task<bool> Evaluate(ReplayEvaluationContext context)
    {
        if (NeedsToReplay(context))
        {
            await _replayCandidatesStorage.Add(new(
                context.Id,
                context.Key,
                new[]
                {
                    new EventTypesChangedReplayCandidateReason(
                        context.State.EventTypes,
                        context.Subscription.EventTypes)
                }));
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
