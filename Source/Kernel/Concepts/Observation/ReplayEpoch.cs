// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the identifier of the replay operation that scheduled a delivery (the replay job's id).
/// </summary>
/// <param name="Value">The inner value.</param>
public record ReplayEpoch(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ReplayEpoch"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert from.</param>
    public static implicit operator ReplayEpoch(Guid value) => new(value);
}
