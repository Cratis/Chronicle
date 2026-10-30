// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the materialization fence.
/// </summary>
/// <param name="Gate">The gate.</param>
/// <param name="Epoch">The epoch.</param>
public record MaterializationFence(GenerationGateId Gate, long Epoch);
