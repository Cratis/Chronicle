// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents the zero-based index of the part to extract when splitting a property value.
/// </summary>
/// <param name="Value">Actual value.</param>
public record SplitPartIndex(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Gets the representation of the first part (index 0).
    /// </summary>
    public static readonly SplitPartIndex First = new(0);

    /// <summary>
    /// Gets the representation of the second part (index 1).
    /// </summary>
    public static readonly SplitPartIndex Second = new(1);

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="SplitPartIndex"/>.
    /// </summary>
    /// <param name="index"><see cref="int"/> to convert from.</param>
    public static implicit operator SplitPartIndex(int index) => new(index);

    /// <summary>
    /// Implicitly convert from <see cref="SplitPartIndex"/> to <see cref="int"/>.
    /// </summary>
    /// <param name="index"><see cref="SplitPartIndex"/> to convert from.</param>
    public static implicit operator int(SplitPartIndex index) => index.Value;
}
