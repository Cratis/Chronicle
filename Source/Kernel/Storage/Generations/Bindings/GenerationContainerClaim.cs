// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the generation container claim.
/// </summary>
/// <param name="Destination">The destination.</param>
/// <param name="Generation">The generation.</param>
/// <param name="State">The state.</param>
public record GenerationContainerClaim(ReadModelContainerName Destination, MaterializationGeneration Generation, MaterializationGenerationState State);
