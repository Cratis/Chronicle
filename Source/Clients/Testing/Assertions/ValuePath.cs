// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Assertions;

/// <summary>
/// Tracks a property, collection element, or dictionary entry in a comparison.
/// </summary>
/// <param name="Value">The displayed path.</param>
internal readonly record struct ValuePath(string Value)
{
    /// <summary>
    /// Appends a member to this path.
    /// </summary>
    /// <param name="name">The member name.</param>
    /// <returns>The nested path.</returns>
    internal ValuePath Member(string name) => new($"{Value}.{name}");

    /// <summary>
    /// Appends an indexed element to this path.
    /// </summary>
    /// <param name="index">The element index or dictionary key.</param>
    /// <returns>The nested path.</returns>
    internal ValuePath Element(object index) => new($"{Value}[{index}]");

    /// <summary>
    /// Describes a difference at this path.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>The mismatch without graph serialization.</returns>
    internal ValueDifference Difference(object? expected, object? actual) =>
        new(Value, ExpectedValueComparer.Describe(expected), ExpectedValueComparer.Describe(actual));
}
