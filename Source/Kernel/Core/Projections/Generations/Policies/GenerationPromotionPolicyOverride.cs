// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.Policies;

/// <summary>
/// Represents optional overrides of an inherited generation policy. Null values retain the inherited setting.
/// </summary>
/// <param name="Mode">The promotion gate override.</param>
/// <param name="RollbackWindow">The rollback window override.</param>
/// <param name="MaxFollowingRetiredGenerations">The retired follower limit override.</param>
/// <param name="DeploymentConditionGracePeriod">The deployment grace period override.</param>
public record GenerationPromotionPolicyOverride(
    PromotionGateMode? Mode = null,
    TimeSpan? RollbackWindow = null,
    int? MaxFollowingRetiredGenerations = null,
    TimeSpan? DeploymentConditionGracePeriod = null);
