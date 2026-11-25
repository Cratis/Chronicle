// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Bounds recursive identity normalization without treating invalid values as normalized nulls.
/// </summary>
/// <param name="Depth">The current traversal depth.</param>
/// <param name="MaximumDepth">The maximum supported traversal depth.</param>
internal sealed record NormalizationScope(int Depth, int MaximumDepth)
{
    /// <summary>
    /// Gets the initial traversal scope.
    /// </summary>
    internal static NormalizationScope Root { get; } = new(0, 64);

    /// <summary>
    /// Normalizes a value only when it is within the supported traversal depth.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>The normalized value, or the unsupported sentinel when the depth is exceeded.</returns>
    internal object? Normalize(object? value) => Depth > MaximumDepth
        ? StructuralKeyNormalizer.Unsupported
        : StructuralKeyNormalizer.Normalize(value, this);

    /// <summary>
    /// Normalizes a child value in the next traversal scope.
    /// </summary>
    /// <param name="value">The child value.</param>
    /// <returns>The normalized child value, or the unsupported sentinel.</returns>
    internal object? NormalizeChild(object? value) => (this with { Depth = Depth + 1 }).Normalize(value);
}
