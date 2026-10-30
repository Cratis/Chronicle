// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.Generations.Policies;

/// <summary>
/// Resolves generation policy under the server gate ceiling. Discovered through the kernel's interface binding convention.
/// </summary>
/// <param name="options">The server configuration.</param>
/// <param name="overrides">The event store and projection overrides.</param>
/// <remarks>
/// Gate strictness is OnCatchUp &lt; Deployment &lt; Operator &lt; Gated, independently of enum numeric values.
/// Overrides may tighten inherited gates, never loosen them. Other settings inherit from server to store to projection.
/// Reducer rollback is disabled until an explicitly configured reducer window can be distinguished from the default.
/// </remarks>
public class GenerationPolicies(IOptions<ChronicleOptions> options, IGenerationPolicyOverrides overrides) : IGenerationPolicies
{
    /// <inheritdoc/>
    public GenerationPromotionPolicy Resolve(EventStoreName eventStore, ProjectionId projectionId, bool isKernelOwned, bool isReducer, PromotionGateMode? requestedGate = null)
    {
        var configuration = options.Value;
        if (!configuration.Features.ProjectionGenerations)
        {
            return new(PromotionGateMode.OnCatchUp, TimeSpan.Zero, 0, TimeSpan.Zero);
        }

        var policy = new GenerationPromotionPolicy(
            configuration.Observers.GenerationPromotion,
            configuration.ReadModels.RollbackWindow,
            configuration.ReadModels.MaxFollowingRetiredGenerations,
            configuration.Observers.DeploymentConditionGracePeriod);
        if (isKernelOwned)
        {
            return policy with { Mode = PromotionGateMode.OnCatchUp, RollbackWindow = TimeSpan.Zero };
        }

        policy = Apply(Apply(policy, overrides.GetForEventStore(eventStore)), overrides.GetForProjection(eventStore, projectionId));
        return policy with
        {
            Mode = Stricter(policy.Mode, requestedGate),
            RollbackWindow = isReducer ? TimeSpan.Zero : policy.RollbackWindow
        };
    }

    static GenerationPromotionPolicy Apply(GenerationPromotionPolicy policy, GenerationPromotionPolicyOverride? policyOverride) =>
        policyOverride is null ? policy : policy with
        {
            Mode = Stricter(policy.Mode, policyOverride.Mode),
            RollbackWindow = policyOverride.RollbackWindow ?? policy.RollbackWindow,
            MaxFollowingRetiredGenerations = policyOverride.MaxFollowingRetiredGenerations ?? policy.MaxFollowingRetiredGenerations,
            DeploymentConditionGracePeriod = policyOverride.DeploymentConditionGracePeriod ?? policy.DeploymentConditionGracePeriod
        };

    static PromotionGateMode Stricter(PromotionGateMode inherited, PromotionGateMode? requested) =>
        requested is { } mode && Strictness(mode) > Strictness(inherited) ? mode : inherited;

    static int Strictness(PromotionGateMode mode) => mode switch
    {
        PromotionGateMode.OnCatchUp => 0,
        PromotionGateMode.Deployment => 1,
        PromotionGateMode.Operator => 2,
        _ => 3
    };
}
