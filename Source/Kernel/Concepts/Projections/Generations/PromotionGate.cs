// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Represents the promotion conditions recorded for a materialization generation.
/// </summary>
/// <param name="Mode">The condition that arms promotion; readiness must still be proven.</param>
/// <param name="ScheduledAt">The optional time before which promotion must not occur.</param>
/// <param name="RequiredVersion">The optional minimum registering client version required for deployment promotion.</param>
/// <param name="IsSticky">Whether the gate is retained after rollback to prevent automatic re-promotion of the displaced generation.</param>
public record PromotionGate(
    PromotionGateMode Mode,
    DateTimeOffset? ScheduledAt = null,
    SemanticVersion? RequiredVersion = null,
    bool IsSticky = false);
