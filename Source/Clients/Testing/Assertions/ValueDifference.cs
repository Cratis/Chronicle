// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Assertions;

/// <summary>
/// Describes a mismatch without serializing an object graph.
/// </summary>
/// <param name="Path">The first mismatching path.</param>
/// <param name="Expected">The expected description.</param>
/// <param name="Actual">The actual description.</param>
internal sealed record ValueDifference(string Path, string? Expected, string? Actual)
{
    /// <inheritdoc/>
    public override string ToString() => $"{Path} expected '{Expected}' but was '{Actual}'";
}
