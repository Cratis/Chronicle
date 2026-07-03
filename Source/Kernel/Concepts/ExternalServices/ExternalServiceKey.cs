// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the compound key for an external service.
/// </summary>
/// <param name="ExternalServiceId">The external service identifier.</param>
/// <param name="EventStore">The event store.</param>
public record ExternalServiceKey(ExternalServiceId ExternalServiceId, EventStoreName EventStore)
{
    /// <summary>
    /// Implicitly convert from <see cref="ExternalServiceKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ExternalServiceKey"/> to convert from.</param>
    public static implicit operator string(ExternalServiceKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(ExternalServiceId, EventStore);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ExternalServiceKey"/> instance.</returns>
    public static ExternalServiceKey Parse(string key) => KeyHelper.Parse<ExternalServiceKey>(key);
}
