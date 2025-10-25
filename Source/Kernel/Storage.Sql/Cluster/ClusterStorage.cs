// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents an implementation of <see cref="IClusterStorage"/> for SQL.
/// </summary>
public class ClusterStorage : IClusterStorage
{
    readonly IServiceProvider _serviceProvider;
    readonly ClusterDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterStorage"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="dbContext">The <see cref="ClusterDbContext"/> to use for storage operations.</param>
    public ClusterStorage(IServiceProvider serviceProvider, ClusterDbContext dbContext)
    {
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
        _dbContext.Database.Migrate();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores() =>
        await _dbContext.EventStores.Select(es => (EventStoreName)es.Name).ToListAsync();

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores() => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventStoreStorage CreateStorageForEventStore(EventStoreName eventStore, SinksFactory sinksFactory)
    {
        var eventStoreDbContext = _serviceProvider.GetRequiredService<EventStoreDbContext>();
        eventStoreDbContext.Database.Migrate();
        return new EventStoreStorage(eventStoreDbContext, eventStore);
    }

    /// <inheritdoc/>
    public Task SaveEventStore(EventStoreName eventStore)
    {
        _dbContext.EventStores.Add(new EventStore { Name = eventStore });
        return _dbContext.SaveChangesAsync();
    }
}
