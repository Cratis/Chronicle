// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks;

/// <summary>
/// Represents a <see cref="IWebhookSigningKeysStorage"/> for webhook signing keys in SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class WebhookSigningKeysStorage(EventStoreName eventStore, IDatabase database) : IWebhookSigningKeysStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Webhooks.WebhookSigningKey>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var keys = await scope.DbContext.WebhookSigningKeys.AsNoTracking().ToListAsync();
        return keys.Select(key => key.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.Observation.Webhooks.WebhookSigningKey key)
    {
        await using var scope = await database.EventStore(eventStore);
        await scope.DbContext.WebhookSigningKeys.Upsert(key.ToSql());
    }
}
