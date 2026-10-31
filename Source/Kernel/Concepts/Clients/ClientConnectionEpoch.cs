// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Clients;

/// <summary>
/// Represents the identifier of one connected period of a client connection.
/// </summary>
/// <remarks>
/// A connection identifier can be reused for a restored client. The epoch tells a notification
/// from an earlier connected period apart from one for the current period.
/// </remarks>
/// <param name="Value">Actual inner value.</param>
public record ClientConnectionEpoch(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents an epoch that has not been set.
    /// </summary>
    public static readonly ClientConnectionEpoch NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ClientConnectionEpoch"/>.
    /// </summary>
    /// <param name="value">Guid to convert from.</param>
    public static implicit operator ClientConnectionEpoch(Guid value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="ClientConnectionEpoch"/> to <see cref="Guid"/>.
    /// </summary>
    /// <param name="value"><see cref="ClientConnectionEpoch"/> to convert from.</param>
    public static implicit operator Guid(ClientConnectionEpoch value) => value.Value;

    /// <summary>
    /// Create a new unique <see cref="ClientConnectionEpoch"/>.
    /// </summary>
    /// <returns>A new <see cref="ClientConnectionEpoch"/>.</returns>
    public static ClientConnectionEpoch New() => new(Guid.NewGuid());
}
