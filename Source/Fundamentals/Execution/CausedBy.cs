// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Execution;

/// <summary>
/// Represents an identifier of an identity that was the root of a cause.
/// </summary>
/// <param name="Value">Actual value.</param>
public record CausedBy(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="CausedBy"/>.
    /// </summary>
    /// <param name="value"><see cref="Guid"/> to convert from.</param>
    public static implicit operator CausedBy(Guid value) => new(value);
}
