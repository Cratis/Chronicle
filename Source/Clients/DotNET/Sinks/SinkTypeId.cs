// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Represents the unique identifier of a type of sink.
/// </summary>
/// <param name="Value">Underlying value.</param>
public record SinkTypeId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the none representation of <see cref="SinkTypeId"/>.
    /// </summary>
    public static readonly SinkTypeId None = "None";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SinkTypeId"/>.
    /// </summary>
    /// <param name="value">String value to convert from.</param>
    public static implicit operator SinkTypeId(string value) => new(value);
}
