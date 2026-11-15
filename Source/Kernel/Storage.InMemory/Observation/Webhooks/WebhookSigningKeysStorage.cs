// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Webhooks;

/// <summary>
/// Represents an in-memory implementation of <see cref="IWebhookSigningKeysStorage"/>.
/// </summary>
public class WebhookSigningKeysStorage : IWebhookSigningKeysStorage
{
    readonly ConcurrentDictionary<WebhookSigningKeyId, WebhookSigningKey> _keys = new();

    /// <inheritdoc/>
    public Task<IEnumerable<WebhookSigningKey>> GetAll() => Task.FromResult<IEnumerable<WebhookSigningKey>>([.. _keys.Values]);

    /// <inheritdoc/>
    public Task Save(WebhookSigningKey key)
    {
        _keys[key.KeyId] = key;
        return Task.CompletedTask;
    }
}
