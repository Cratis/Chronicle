// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the generation reservation.
/// </summary>
/// <param name="Result">The result.</param>
/// <param name="Record">The record.</param>
public record GenerationReservation(GenerationReservationResult Result, MaterializationGenerationRecord? Record = null);
