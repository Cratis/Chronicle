// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents the unique identifier for a connection for a client.
/// </summary>
/// <param name="Value">Actual inner value.</param>
public record ConnectionId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents a connection identifier that has not been set.
    /// </summary>
    public static readonly ConnectionId NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator ConnectionId(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="ConnectionId"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="ConnectionId"/> to convert from.</param>
    public static implicit operator string(ConnectionId value) => value.ToString();

    /// <summary>
    /// Create a new unique <see cref="ConnectionId"/>.
    /// </summary>
    /// <returns>A new <see cref="ConnectionId"/>.</returns>
    public static ConnectionId New() => new(Guid.NewGuid().ToString());
}
