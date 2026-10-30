// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the materialization publication.
/// </summary>
/// <param name="Result">The result.</param>
/// <param name="Active">The active.</param>
/// <param name="Epoch">The epoch.</param>
public record MaterializationPublication(MaterializationPublicationResult Result, MaterializationGeneration Active, ServingEpoch Epoch);
