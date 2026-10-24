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
    public string Value => Format(Key.Value, Generation.Value);

    /// <summary>
    /// Formats the gate identity shared by bindings and sink fencing.
    /// </summary>
    /// <param name="key">The materialization key.</param>
    /// <param name="generation">The generation.</param>
    /// <returns>The gate identity.</returns>
    public static string Format(string key, ulong generation) => $"{key}#{generation}";
}
