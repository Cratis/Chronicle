// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the compound key for a webhook.
/// </summary>
/// <param name="WebhookId">The webhook identifier.</param>
/// <param name="EventStore">The event store.</param>
public record WebhookKey(WebhookId WebhookId, EventStoreName EventStore)
{
    /// <summary>
    /// Implicitly convert from <see cref="WebhookKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="WebhookKey"/> to convert from.</param>
    public static implicit operator string(WebhookKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(WebhookId, EventStore);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="WebhookKey"/> instance.</returns>
    public static WebhookKey Parse(string key) => KeyHelper.Parse<WebhookKey>(key);
}
