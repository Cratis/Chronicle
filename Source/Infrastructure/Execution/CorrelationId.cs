// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Execution;

/// <summary>
/// Represents an identifier for correlation.
/// </summary>
/// <param name="Value">Actual value.</param>
public record CorrelationId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="CorrelationId"/>.
    /// </summary>
    /// <param name="id"><see cref="string"/> to convert from.</param>
    /// <returns>A new <see cref="CorrelationId"/>.</returns>
    public static implicit operator CorrelationId(string id) => new(id);

    /// <summary>
    /// Create a new <see cref="CorrelationId"/> based on a new <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="CorrelationId"/>.</returns>
    public static CorrelationId New() => new(Guid.NewGuid().ToString());
}
