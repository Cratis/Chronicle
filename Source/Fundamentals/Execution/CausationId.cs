// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Execution;

/// <summary>
/// Represents an identifier for correlation.
/// </summary>
/// <param name="Value">Actual value.</param>
public record CausationId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents a system cause.
    /// </summary>
    public static readonly CausationId System = "System";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="CausationId"/>.
    /// </summary>
    /// <param name="id"><see cref="string"/> to convert from.</param>
    /// <returns>A new <see cref="CausationId"/>.</returns>
    public static implicit operator CausationId(string id) => new(id);
}
