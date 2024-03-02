// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Recommendations;
using Aksio.Cratis.Kernel.Observation.Replaying;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents an implementation of <see cref="IReplayEvaluator"/>.
/// </summary>
public class ReplayEvaluator : IReplayEvaluator
{
    readonly IGrainFactory _grainFactory;
    readonly MicroserviceId _microserviceId;
    readonly TenantId _tenantId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayEvaluator"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the evaluator is for.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the evaluator is for.</param>
    public ReplayEvaluator(
        IGrainFactory grainFactory,
        MicroserviceId microserviceId,
        TenantId tenantId)
    {
        _grainFactory = grainFactory;
        _microserviceId = microserviceId;
        _tenantId = tenantId;
    }

    /// <inheritdoc/>
    public async Task<bool> Evaluate(ReplayEvaluationContext context)
    {
        if (NeedsToReplay(context))
        {
            var recommendationsManager = _grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(_microserviceId, _tenantId));
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
