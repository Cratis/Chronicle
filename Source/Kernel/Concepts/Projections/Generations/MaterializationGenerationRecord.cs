// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Represents the catalog record of a <see cref="MaterializationGeneration"/>.
/// </summary>
/// <param name="Identity">The <see cref="MaterializationGenerationIdentity"/> the generation is reserved under.</param>
/// <param name="State">The <see cref="MaterializationGenerationState"/> of the generation.</param>
/// <param name="Revision">The <see cref="MaterializationRevision"/> the generation is built from.</param>
/// <param name="CreatedAt">When the generation was reserved.</param>
/// <param name="SupersededBy">The generation that superseded this one, if any.</param>
/// <param name="PromotedAt">When the generation most recently became Active, if known.</param>
/// <param name="RetiredAt">When the generation most recently became Retired, if known.</param>
/// <param name="RollbackDeadline">The exclusive end of the generation's rollback window, if any.</param>
/// <param name="PromotionGate">The optional conditions governing promotion.</param>
/// <param name="RegisteringVersion">The version of the client that registered the generation's definition, if known.</param>
/// <param name="RolledBackFrom">The generation displaced by the rollback that activated this generation, if any.</param>
public record MaterializationGenerationRecord(
    MaterializationGenerationIdentity Identity,
    MaterializationGenerationState State,
    MaterializationRevision Revision,
    DateTimeOffset CreatedAt,
    MaterializationGeneration? SupersededBy = default,
    DateTimeOffset? PromotedAt = null,
    DateTimeOffset? RetiredAt = null,
    DateTimeOffset? RollbackDeadline = null,
    PromotionGate? PromotionGate = null,
    SemanticVersion? RegisteringVersion = null,
    MaterializationGeneration? RolledBackFrom = null)
{
    /// <summary>
    /// Check whether the generation is Retired and its rollback window is still open.
    /// </summary>
    /// <param name="now">The time at which to check eligibility.</param>
    /// <returns>True only for a Retired generation with a deadline later than <paramref name="now"/>.</returns>
    /// <remarks>This checks lifecycle eligibility only, not coverage, fencing, compliance or reader compatibility.</remarks>
    public bool IsRollbackEligible(DateTimeOffset now) =>
        State == MaterializationGenerationState.Retired && RollbackDeadline is { } deadline && deadline > now;
}
