// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Defines the per-event-store ring of webhook signing keys. The grain key is the event store name.
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
}
