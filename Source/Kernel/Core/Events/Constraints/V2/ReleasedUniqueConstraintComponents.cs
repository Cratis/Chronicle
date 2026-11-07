// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Carries released components or an explicit decision not to claim or remove an erased value.
/// </summary>
/// <param name="Components">The ordered components; empty when release requires skipping the operation.</param>
/// <param name="Outcome">Whether components are readable or an affected boundary was erased.</param>
internal record ReleasedUniqueConstraintComponents(
    IReadOnlyList<UniqueConstraintComponent> Components,
    UniqueConstraintReleaseOutcome Outcome)
{
    /// <summary>
    /// Gets whether the operation must be skipped without invoking the encoder.
    /// </summary>
    internal bool ShouldSkipClaimOrRemoval => Outcome == UniqueConstraintReleaseOutcome.NoClaimErased;
}
