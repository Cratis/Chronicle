// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Admission;

/// <summary>Checks uniform protocol support; this is not old-binary exclusion proof.</summary>
public interface IGenerationMembershipProbe
{
    /// <summary>Checks all current members against the target.</summary>
    /// <param name="target">The requested floor.</param>
    /// <returns>Uniformity evidence, or an explicit unknown/refusal.</returns>
    Task<GenerationMembershipEvidence> Probe(GenerationFloor target);
}
