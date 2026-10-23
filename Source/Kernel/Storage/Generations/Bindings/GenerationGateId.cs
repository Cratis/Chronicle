// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the generation gate id.
/// </summary>
/// <param name="Key">The key.</param>
/// <param name="Generation">The generation.</param>
public record GenerationGateId(MaterializationKey Key, MaterializationGeneration Generation)
{
    /// <summary>
    /// Represents the value.
    /// </summary>
    public string Value => $"{Key.Value}#{Generation.Value}";
}
