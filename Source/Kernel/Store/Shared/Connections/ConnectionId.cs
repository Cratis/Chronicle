// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Connections;

/// <summary>
/// Represents the unique identifier for a connection for a client.
/// </summary>
/// <param name="Value">Actual inner value.</param>
public record ConnectionId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="ConnectionId"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="ConnectionId"/> to convert from.</param>
    public static implicit operator string(ConnectionId value) => value.ToString();
}
