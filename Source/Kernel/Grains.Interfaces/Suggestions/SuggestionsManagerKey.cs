// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Represents the key for a <see cref="ISuggestionsManager"/>.
/// </summary>
/// <param name="MicroserviceId">The Microservice the job is for.</param>
/// <param name="TenantId">The tenant the job is for.</param>
public record SuggestionsManagerKey(MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <summary>
    /// Represents an unset key.
    /// </summary>
    public static readonly SuggestionsManagerKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet);

    /// <summary>
    /// Implicitly convert from string to <see cref="SuggestionsManagerKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator SuggestionsManagerKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="SuggestionsManagerKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(SuggestionsManagerKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="SuggestionsManagerKey"/> instance.</returns>
    public static SuggestionsManagerKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        return new(microserviceId, tenantId);
    }
}
