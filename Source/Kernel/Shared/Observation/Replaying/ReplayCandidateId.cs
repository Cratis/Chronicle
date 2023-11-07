// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Observation.Replaying;

/// <summary>
/// Represents the unique identifier of a <see cref="ReplayCandidate"/>.
/// </summary>
/// <param name="Value">The inner value.</param>
public record ReplayCandidateId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ReplayCandidateId"/>.
    /// </summary>
    /// <param name="value">A new <see cref="ReplayCandidateId"/>.</param>
    public static implicit operator ReplayCandidateId(Guid value) => new(value);

    /// <summary>
    /// Create a new <see cref="ReplayCandidateId"/>.
    /// </summary>
    /// <returns>A new <see cref="ReplayCandidateId"/>.</returns>
    public static ReplayCandidateId New() => new(Guid.NewGuid());
}
