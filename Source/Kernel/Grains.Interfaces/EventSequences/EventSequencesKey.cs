// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents the key for <see cref="IEventSequences"/>.
/// </summary>
/// <param name="MicroserviceId">The <see cref="MicroserviceId"/> part.</param>
/// <param name="TenantId">The <see cref="TenantId"/> part.</param>
public record EventSequencesKey(MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <summary>
    /// The key when not set.
    /// </summary>
    public static readonly EventSequencesKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="EventSequencesKey"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(EventSequencesKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventSequencesKey"/>.
    /// </summary>
    /// <param name="key">String to convert from.</param>
    public static implicit operator EventSequencesKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a <see cref="EventSequencesKey"/> from a string.
    /// </summary>
    /// <param name="key">String to parse.</param>
    /// <returns>A parsed <see cref="EventSequencesKey"/>.</returns>
    public static EventSequencesKey Parse(string key)
    {
        var part = key.Split('+');
        return new EventSequencesKey(part[0], part[1]);
    }
}
