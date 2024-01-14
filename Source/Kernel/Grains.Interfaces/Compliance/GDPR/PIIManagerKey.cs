// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Compliance.GDPR;

/// <summary>
/// Represents the key for <see cref="IPIIManager"/>.
/// </summary>
/// <param name="MicroserviceId">The <see cref="MicroserviceId"/> part.</param>
/// <param name="TenantId">The <see cref="TenantId"/> part.</param>
public record PIIManagerKey(MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <summary>
    /// The key when not set.
    /// </summary>
    public static readonly PIIManagerKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="PIIManagerKey"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(PIIManagerKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="PIIManagerKey"/>.
    /// </summary>
    /// <param name="key">String to convert from.</param>
    public static implicit operator PIIManagerKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a <see cref="PIIManagerKey"/> from a string.
    /// </summary>
    /// <param name="key">String to parse.</param>
    /// <returns>A parsed <see cref="PIIManagerKey"/>.</returns>
    public static PIIManagerKey Parse(string key)
    {
        var part = key.Split('+');
        return new PIIManagerKey(part[0], part[1]);
    }
}
