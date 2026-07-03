// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Storage.ExternalServices;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ExternalServices;

/// <summary>
/// Represents a <see cref="IExternalServiceDefinitionsStorage"/> for external service definitions in SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ExternalServiceDefinitionsStorage(EventStoreName eventStore, IDatabase database) : IExternalServiceDefinitionsStorage, IDisposable
{
    readonly ReplaySubject<IEnumerable<Concepts.ExternalServices.ExternalServiceDefinition>> _subject = new(1);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.ExternalServices.ExternalServiceDefinition>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var definitions = await scope.DbContext.ExternalServiceDefinitions.ToListAsync();
        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.ExternalServices.ExternalServiceDefinition>> ObserveAll() => _subject;

    /// <inheritdoc/>
    public async Task<bool> Has(ExternalServiceId id)
    {
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.ExternalServiceDefinitions.AnyAsync(r => r.Id == id.Value);
    }

    /// <inheritdoc/>
    public async Task<Concepts.ExternalServices.ExternalServiceDefinition> Get(ExternalServiceId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var definition = await scope.DbContext.ExternalServiceDefinitions.SingleAsync(definition => definition.Id == id.Value);
        return definition.ToKernel();
    }

    /// <inheritdoc/>
    public async Task Delete(ExternalServiceId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var definition = await scope.DbContext.ExternalServiceDefinitions.SingleOrDefaultAsync(d => d.Id == id.Value);
        if (definition is not null)
        {
            scope.DbContext.ExternalServiceDefinitions.Remove(definition);
            await scope.DbContext.SaveChangesAsync();
            await NotifyChange();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.ExternalServices.ExternalServiceDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);
        await scope.DbContext.ExternalServiceDefinitions.Upsert(definition.ToSql());
        await scope.DbContext.SaveChangesAsync();
        await NotifyChange();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _subject.Dispose();
        GC.SuppressFinalize(this);
    }

    async Task NotifyChange()
    {
        var all = await GetAll();
        _subject.OnNext(all);
    }
}
