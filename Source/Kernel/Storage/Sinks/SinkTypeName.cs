// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Storage.Sinks;

/// <summary>
/// Represents a friendly name for a type of <see cref="ISink"/>.
/// </summary>
/// <param name="Value">Underlying value.</param>
public record SinkTypeName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/>  to <see cref="SinkTypeId"/>.
    /// </summary>
    /// <param name="value">String value to convert from.</param>
    public static implicit operator SinkTypeName(string value) => new(value);
}
