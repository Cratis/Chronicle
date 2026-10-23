// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Admission;

/// <summary>The dormant default refuses advancements until a real membership probe is installed.</summary>
public sealed class UnknownGenerationMembershipProbe : IGenerationMembershipProbe
{
    /// <inheritdoc/>
    public Task<GenerationMembershipEvidence> Probe(GenerationFloor target) => Task.FromResult(new GenerationMembershipEvidence(false, "Generation membership support is unknown."));
}
