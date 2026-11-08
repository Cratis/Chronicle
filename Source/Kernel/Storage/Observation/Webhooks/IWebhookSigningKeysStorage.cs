// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.Observation.Webhooks;

/// <summary>
/// Defines a system for storing <see cref="WebhookSigningKey">webhook signing keys</see> of an event store.
/// </summary>
/// <remarks>
/// The private material of a key is only ever stored as ciphertext in <see cref="WebhookSigningKey.EncryptedSecret"/>.
/// </remarks>
public interface IWebhookSigningKeysStorage
{
    /// <summary>
    /// Get all <see cref="WebhookSigningKey">signing keys</see> of the event store, in any status.
    /// </summary>
    /// <returns>A collection of <see cref="WebhookSigningKey"/>.</returns>
    Task<IEnumerable<WebhookSigningKey>> GetAll();

    /// <summary>
    /// Save a <see cref="WebhookSigningKey"/>, replacing any stored key with the same <see cref="WebhookSigningKey.KeyId"/>.
    /// </summary>
    /// <param name="key">The <see cref="WebhookSigningKey"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(WebhookSigningKey key);
}
