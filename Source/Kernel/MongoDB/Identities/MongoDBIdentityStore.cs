// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Identities;

/// <summary>
/// Represents an implementation of <see cref="IIdentityStore"/> using MongoDB.
/// </summary>
[SingletonPerTenant]
public class MongoDBIdentityStore : IIdentityStore
{
    readonly ITenantDatabase _database;
    Dictionary<IdentityId, Identity> _causedBy = new();
    Dictionary<string, IdentityId> _causedByIdsBySubject = new();
    Dictionary<string, IdentityId> _causedByIdsByUserName = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBIdentityStore"/> class.
    /// </summary>
    /// <param name="database">The database for the current tenant.</param>
    public MongoDBIdentityStore(ITenantDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public async Task Populate()
    {
        var result = await GetCollection().FindAsync(_ => true);
        var allCausedBy = await result.ToListAsync();
        _causedBy = allCausedBy.ToDictionary(_ => (IdentityId)_.Id, _ => new Identity(_.Subject, _.Name, _.UserName));
        _causedByIdsBySubject = _causedBy.ToDictionary(_ => _.Value.Subject, _ => _.Key);
        _causedByIdsByUserName = _causedBy.ToDictionary(_ => _.Value.UserName, _ => _.Key);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<IdentityId>> GetChainFor(Identity causedBy)
    {
        var chain = new List<IdentityId>();
        Identity? current = causedBy;
        while (current is not null)
        {
            chain.Add(await GetSingleFor(current));
            current = current.OnBehalfOf;
        }

        return chain.ToImmutableList();
    }

    /// <inheritdoc/>
    public async Task<Identity> GetFor(IEnumerable<IdentityId> chain)
    {
        var chainArray = chain.ToArray();
        var current = Identity.NotSet;
        Identity? previous = null;
        for (var chainIndex = chainArray.Length - 1; chainIndex >= 0; chainIndex--)
        {
            var causedById = chainArray[chainIndex];
            current = await GetSingleFor(causedById) with { OnBehalfOf = previous };
        }

        return current;
    }

    /// <inheritdoc/>
    public async Task<Identity> GetSingleFor(IdentityId causedById)
    {
        if (!await HasFor(causedById))
        {
            throw new UnknownIdentityIdentifier(causedById);
        }

        return _causedBy[causedById];
    }

    /// <inheritdoc/>
    public async Task<IdentityId> GetSingleFor(Identity causedBy)
    {
        if (TryGetSingleFor(causedBy, out var causedById)) return causedById;
        await Populate();
        if (TryGetSingleFor(causedBy, out causedById)) return causedById;

        causedById = Guid.NewGuid();
        _causedByIdsByUserName[causedBy.UserName] = causedById;
        _causedBy[causedById] = causedBy;
        return causedById;
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(IdentityId causedById)
    {
        if (_causedBy.ContainsKey(causedById)) return true;

        await Populate();

        return _causedBy.ContainsKey(causedById);
    }

    bool TryGetSingleFor(Identity causedBy, out IdentityId causedById)
    {
        if (!string.IsNullOrEmpty(causedBy.Subject))
        {
            if (_causedByIdsBySubject.ContainsKey(causedBy.Subject))
            {
                causedById = _causedByIdsBySubject[causedBy.Subject];
                return true;
            }
        }
        else if (!string.IsNullOrEmpty(causedBy.UserName))
        {
            if (_causedByIdsByUserName.ContainsKey(causedBy.UserName))
            {
                causedById = _causedByIdsByUserName[causedBy.UserName];
                return true;
            }
        }
        causedById = Guid.Empty;

        return false;
    }

    IMongoCollection<MongoDBIdentity> GetCollection() => _database.GetCollection<MongoDBIdentity>(CollectionNames.Identities);
}
