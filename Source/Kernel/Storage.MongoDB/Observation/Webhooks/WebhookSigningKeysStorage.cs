// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation.Webhooks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks;

/// <summary>
/// Represents a <see cref="IWebhookSigningKeysStorage"/> for webhook signing keys in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class WebhookSigningKeysStorage(IEventStoreDatabase eventStoreDatabase) : IWebhookSigningKeysStorage
{
    IMongoCollection<WebhookSigningKey> Collection => eventStoreDatabase.GetCollection<WebhookSigningKey>(WellKnownCollectionNames.WebhookSigningKeys);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Webhooks.WebhookSigningKey>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<WebhookSigningKey>.Empty);
        var keys = await result.ToListAsync();
        return keys.Select(key => key.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task Save(Concepts.Observation.Webhooks.WebhookSigningKey key) =>
        Collection.ReplaceOneAsync(
            filter: document => document.Id == key.KeyId,
            replacement: key.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
}
