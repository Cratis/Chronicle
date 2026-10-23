// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a part of a composite key holds a nested object instead of a flat value.
/// </summary>
/// <param name="part">The name of the part that holds the nested value.</param>
public class NestedCompositeKeyPartsAreNotSupported(string part) : Exception($"The composite key part '{part}' holds a nested value. Composite key parts must be flat values.")
{
    /// <summary>
    /// Gets the name of the part that holds the nested value.
    /// </summary>
    public string Part { get; } = part;
}
