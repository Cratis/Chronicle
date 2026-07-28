// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Computes hashes that are identical for the same input in every process and on every run.
/// </summary>
/// <remarks>
/// <see cref="string.GetHashCode()"/> is seeded randomly per process in .NET, so it cannot be used anywhere the same
/// input has to land in the same bucket across silos, activations and restarts. This is the single implementation
/// every such bucketing in the kernel shares; changing it changes every bucket assignment at once, which is only
/// safe because none of them are persisted.
/// </remarks>
internal static class StableHash
{
    const uint OffsetBasis = 2166136261;
    const uint Prime = 16777619;

    /// <summary>
    /// Computes the FNV-1a hash of a string, over its UTF-16 code units.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <returns>The hash of the value.</returns>
    internal static uint Of(string value)
    {
        var hash = OffsetBasis;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= Prime;
        }

        return hash;
    }
}
