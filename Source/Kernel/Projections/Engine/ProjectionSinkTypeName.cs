// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents a friendly name for a type of <see cref="IProjectionSink"/>.
/// </summary>
/// <param name="Value">Underlying value.</param>
public record ProjectionSinkTypeName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/>  to <see cref="ProjectionSinkTypeId"/>.
    /// </summary>
    /// <param name="value">String value to convert from.</param>
    public static implicit operator ProjectionSinkTypeName(string value) => new(value);
}
