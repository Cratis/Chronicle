// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.Policies;

/// <summary>
/// Represents the effective promotion and rollback policy for a projection generation.
/// </summary>
/// <param name="Mode">The promotion gate.</param>
/// <param name="RollbackWindow">How long a retired generation keeps following for rollback.</param>
/// <param name="MaxFollowingRetiredGenerations">The maximum number of retired followers.</param>
/// <param name="DeploymentConditionGracePeriod">How long the deployment condition must hold.</param>
public record GenerationPromotionPolicy(
    PromotionGateMode Mode,
    TimeSpan RollbackWindow,
    int MaxFollowingRetiredGenerations,
    TimeSpan DeploymentConditionGracePeriod);
