// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Persistence.EventSequences;

/// <summary>
/// Represents the key for an event sequence.
/// </summary>
/// <param name="MicroserviceId">The <see cref="MicroserviceId"/> part.</param>
/// <param name="TenantId">The <see cref="TenantId"/> part.</param>
public record EventSequenceKey(MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <summary>
    /// The key when not set.
    /// </summary>
    public static readonly EventSequenceKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="EventSequenceKey"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(EventSequenceKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventSequenceKey"/>.
    /// </summary>
    /// <param name="key">String to convert from.</param>
    public static implicit operator EventSequenceKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a <see cref="EventSequenceKey"/> from a string.
    /// </summary>
    /// <param name="key">String to parse.</param>
    /// <returns>A parsed <see cref="EventSequenceKey"/>.</returns>
    public static EventSequenceKey Parse(string key)
    {
        var part = key.Split('+');
        return new EventSequenceKey(part[0], part[1]);
    }
}
