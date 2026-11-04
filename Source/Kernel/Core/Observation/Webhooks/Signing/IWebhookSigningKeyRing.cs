// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Defines the per-event-store ring of webhook signing keys and per-webhook HMAC secrets. The grain key is the event store name.
/// </summary>
public interface IWebhookSigningKeyRing : IGrainWithStringKey
{
    /// <summary>
    /// Gets the keys valid for signing and verification: the active key first, then every key still in its overlap window.
    /// </summary>
    /// <remarks>
    /// Generates the first key when the event store has none, and retires keys whose overlap window has ended.
    /// The private material is returned as ciphertext only.
    /// </remarks>
    /// <returns>The valid <see cref="WebhookSigningKey">signing keys</see>.</returns>
    Task<IEnumerable<WebhookSigningKey>> GetSigningKeys();

    /// <summary>
    /// Rotates the active key, keeping the previous key valid for the overlap window.
    /// </summary>
    /// <param name="overlap">How long the previous key stays valid. Zero retires it immediately.</param>
    /// <returns>The new active <see cref="WebhookSigningKey"/>.</returns>
    /// <exception cref="InvalidWebhookSigningKeyOverlap">Thrown when the overlap is negative or beyond <see cref="WebhookSigningKeyOverlap.Maximum"/>.</exception>
    Task<WebhookSigningKey> Rotate(WebhookSigningKeyOverlap overlap);

    /// <summary>
    /// Gets the webhook's active secret first, followed by its open overlap secrets, as ciphertext only.
    /// </summary>
    /// <remarks>Retires expired overlaps and never generates a secret on demand.</remarks>
    /// <param name="webhook">The webhook whose secrets to get.</param>
    /// <returns>The webhook's valid secrets, or an empty collection when none exist.</returns>
    Task<IEnumerable<WebhookSigningKey>> GetWebhookSecrets(WebhookId webhook);

    /// <summary>
    /// Creates a webhook's first secret or rotates its active secret, retaining the previous one for the overlap window.
    /// </summary>
    /// <remarks>
    /// Returns ciphertext only. The caller handing the secret to the receiver decrypts it locally with <see cref="IEncryption"/>
    /// for one-time delivery; there is no plaintext read surface.
    /// </remarks>
    /// <param name="webhook">The webhook whose secret to rotate.</param>
    /// <param name="overlap">How long the previous secret stays valid. Zero retires it immediately.</param>
    /// <returns>The new active secret, with its private material encrypted.</returns>
    /// <exception cref="InvalidWebhookSigningKeyOverlap">Thrown when the overlap is negative or beyond <see cref="WebhookSigningKeyOverlap.Maximum"/>.</exception>
    Task<WebhookSigningKey> RotateWebhookSecret(WebhookId webhook, WebhookSigningKeyOverlap overlap);

    /// <summary>
    /// Retires and erases every active or overlap secret of a webhook.
    /// </summary>
    /// <param name="webhook">The webhook whose secrets to retire.</param>
    /// <returns>A task representing retirement. Repeated calls have no further effect.</returns>
    Task RetireWebhookSecrets(WebhookId webhook);
}
