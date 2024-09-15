// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="INamespaceStorage"/>.
/// </summary>
/// <param name="database"><see cref="IDatabase"/> for storage.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class NamespaceStorage(
    IEventStoreDatabase database,
    ILogger<NamespaceStorage> logger) : INamespaceStorage
{
    /// <inheritdoc/>
    public async Task Ensure(EventStoreNamespaceName name)
    {
        var result = await GetCollection().FindAsync(Builders<MongoDBNamespace>.Filter.Eq(_ => _.Name, (string)name));
        if (!result.Any())
        {
            await Create(name, DateTimeOffset.UtcNow);
        }
    }

    /// <inheritdoc/>
    public Task Create(EventStoreNamespaceName name, DateTimeOffset created)
    {
        logger.Creating(name);

        var @namespace = new MongoDBNamespace
        {
            Id = Guid.NewGuid(),
            Name = name,
            Created = created
        };

        return GetCollection().ReplaceOneAsync(
            Builders<MongoDBNamespace>.Filter.Eq(_ => _.Name, (string)name),
            @namespace,
            new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task Delete(EventStoreNamespaceName name)
    {
        logger.Deleting(name);

        await GetCollection().DeleteOneAsync(Builders<MongoDBNamespace>.Filter.Eq(_ => _.Name, (string)name));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NamespaceState>> GetAll()
    {
        var result = await GetCollection().FindAsync(_ => true);
        var namespaces = await result.ToListAsync();
        return namespaces.Select(_ => new NamespaceState(_.Id, _.Name, _.Created));
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<NamespaceState>> ObserveNamespaces() =>
         new TransformingSubject<IEnumerable<MongoDBNamespace>, IEnumerable<NamespaceState>>(
            GetCollection().Observe(),
            _ => _.Select(_ => new NamespaceState(_.Id, _.Name, _.Created)));

    IMongoCollection<MongoDBNamespace> GetCollection() => database.GetCollection<MongoDBNamespace>(WellKnownCollectionNames.Namespaces);
}
