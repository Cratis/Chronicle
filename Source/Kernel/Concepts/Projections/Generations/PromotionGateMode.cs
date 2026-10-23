// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Represents the condition that arms promotion of a materialization generation.
/// </summary>
public enum PromotionGateMode
{
    /// <summary>
    /// Promote automatically after catch-up and readiness are proven.
    /// </summary>
    OnCatchUp = 0,

    /// <summary>
    /// Require an explicit operator request before promotion.
    /// </summary>
    Operator = 1,

    /// <summary>
    /// Require connected declaring clients to meet the required version before promotion.
    /// </summary>
    Deployment = 2,

    /// <summary>
    /// Hold promotion until the gate is explicitly released.
    /// </summary>
    Gated = 3
}
