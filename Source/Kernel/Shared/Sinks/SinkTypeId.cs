// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Sinks;

/// <summary>
/// Represents the unique identifier of a type of sink.
/// </summary>
/// <param name="Value">Underlying value.</param>
public record SinkTypeId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> representation of a <see cref="Guid"/> to <see cref="SinkTypeId"/>.
    /// </summary>
    /// <param name="value">String value to convert from.</param>
    public static implicit operator SinkTypeId(string value) => new(Guid.Parse(value));
}
