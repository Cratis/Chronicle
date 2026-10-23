// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>
/// Identifies a reason initialization cannot proceed.
/// </summary>
public enum GenerationInitializationBlockerKind
{
    /// <summary>
    /// ExclusionNotProven.
    /// </summary>
    ExclusionNotProven = 0,

    /// <summary>
    /// FloorNotEstablished.
    /// </summary>
    FloorNotEstablished = 1,

    /// <summary>
    /// MembershipNotUniform.
    /// </summary>
    MembershipNotUniform = 2,

    /// <summary>
    /// LegacyReplaySessionUnresolved.
    /// </summary>
    LegacyReplaySessionUnresolved = 3,

    /// <summary>
    /// LegacyReplayJobUnresolved.
    /// </summary>
    LegacyReplayJobUnresolved = 4,

    /// <summary>
    /// ContradictoryLegacyState.
    /// </summary>
    ContradictoryLegacyState = 5,

    /// <summary>
    /// UnknownLegacyState.
    /// </summary>
    UnknownLegacyState = 6,

    /// <summary>
    /// CatalogUnavailable.
    /// </summary>
    CatalogUnavailable = 7
}
