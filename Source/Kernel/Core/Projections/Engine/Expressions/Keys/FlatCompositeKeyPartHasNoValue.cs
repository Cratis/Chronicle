// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// The exception that is thrown when an event does not provide a value for a part of a flat composite key.
/// </summary>
/// <param name="part">The key part without a value.</param>
public class FlatCompositeKeyPartHasNoValue(string part) : Exception($"The event provides no value for the flat composite key part '{part}'. Every part of the key must have a value; the event cannot be used to identify the read model.")
{
    /// <summary>
    /// Gets the key part without a value.
    /// </summary>
    public string Part { get; } = part;
}
