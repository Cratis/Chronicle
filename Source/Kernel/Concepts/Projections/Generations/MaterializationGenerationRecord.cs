// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Represents the catalog record of a <see cref="MaterializationGeneration"/>.
/// </summary>
/// <param name="Identity">The <see cref="MaterializationGenerationIdentity"/> the generation is reserved under.</param>
/// <param name="State">The <see cref="MaterializationGenerationState"/> of the generation.</param>
/// <param name="Revision">The <see cref="MaterializationRevision"/> the generation is built from.</param>
/// <param name="CreatedAt">When the generation was reserved.</param>
/// <param name="SupersededBy">The generation that superseded this one, if any.</param>
public record MaterializationGenerationRecord(
    MaterializationGenerationIdentity Identity,
    MaterializationGenerationState State,
    MaterializationRevision Revision,
    DateTimeOffset CreatedAt,
    MaterializationGeneration? SupersededBy = default);
