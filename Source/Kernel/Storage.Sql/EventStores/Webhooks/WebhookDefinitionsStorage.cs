// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks;

/// <summary>
/// Represents a <see cref="IWebhookDefinitionsStorage"/> for webhook definitions in SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class WebhookDefinitionsStorage(EventStoreName eventStore, IDatabase database) : IWebhookDefinitionsStorage
{
    readonly ReplaySubject<IEnumerable<Concepts.Observation.Webhooks.WebhookDefinition>> _subject = new(1);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Webhooks.WebhookDefinition>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var definitions = await scope.DbContext.WebhookDefinitions.ToListAsync();
        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.Observation.Webhooks.WebhookDefinition>> ObserveAll() => _subject;

    /// <inheritdoc/>
    public async Task<bool> Has(WebhookId id)
    {
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.WebhookDefinitions.AnyAsync(r => r.Id == id.Value);
    }

    /// <inheritdoc/>
    public async Task<Concepts.Observation.Webhooks.WebhookDefinition> Get(WebhookId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var definition = await scope.DbContext.WebhookDefinitions.SingleAsync(definition => definition.Id == id.Value);
        return definition.ToKernel();
    }

    /// <inheritdoc/>
    public async Task Delete(WebhookId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var definition = await scope.DbContext.WebhookDefinitions.SingleOrDefaultAsync(d => d.Id == id.Value);
        if (definition is not null)
        {
            scope.DbContext.WebhookDefinitions.Remove(definition);
            await scope.DbContext.SaveChangesAsync();
            await NotifyChange();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.Observation.Webhooks.WebhookDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);
        await scope.DbContext.WebhookDefinitions.Upsert(definition.ToSql());
        await scope.DbContext.SaveChangesAsync();
        await NotifyChange();
    }

    async Task NotifyChange()
    {
        var all = await GetAll();
        _subject.OnNext(all);
    }
}
