// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Auditing;

/// <summary>
/// Represents an implementation of <see cref="ICausedByStore"/> using MongoDB.
/// </summary>
[SingletonPerTenant]
public class MongoDBCausedByStore : ICausedByStore
{
    readonly ITenantDatabase _database;
    Dictionary<CausedById, CausedBy> _causedBy = new();
    Dictionary<string, CausedById> _causedByIdsBySubject = new();
    Dictionary<string, CausedById> _causedByIdsByUserName = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBCausedByStore"/> class.
    /// </summary>
    /// <param name="database">The database for the current tenant.</param>
    public MongoDBCausedByStore(ITenantDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public async Task Populate()
    {
        var result = await GetCollection().FindAsync(_ => true);
        var allCausedBy = await result.ToListAsync();
        _causedBy = allCausedBy.ToDictionary(_ => (CausedById)_.Id, _ => new CausedBy(_.Subject, _.Name, _.UserName));
        _causedByIdsBySubject = _causedBy.ToDictionary(_ => _.Value.Subject, _ => _.Key);
        _causedByIdsByUserName = _causedBy.ToDictionary(_ => _.Value.UserName, _ => _.Key);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<CausedById>> GetChainFor(CausedBy causedBy)
    {
        var chain = new List<CausedById>();
        CausedBy? current = causedBy;
        while (current is not null)
        {
            chain.Add(await GetSingleFor(current));
            current = current.OnBehalfOf;
        }

        return chain.ToImmutableList();
    }

    /// <inheritdoc/>
    public async Task<CausedBy> GetFor(IEnumerable<CausedById> chain)
    {
        var chainArray = chain.ToArray();
        CausedBy current = CausedBy.NotSet;
        CausedBy? previous = null;
        for (var chainIndex = chainArray.Length - 1; chainIndex >= 0; chainIndex--)
        {
            var causedById = chainArray[chainIndex];
            current = await GetSingleFor(causedById) with { OnBehalfOf = previous };
        }

        return current;
    }

    /// <inheritdoc/>
    public async Task<CausedBy> GetSingleFor(CausedById causedById)
    {
        if (!await HasFor(causedById))
        {
            throw new UnknownCausedByIdentifier(causedById);
        }

        return _causedBy[causedById];
    }

    /// <inheritdoc/>
    public async Task<CausedById> GetSingleFor(CausedBy causedBy)
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
    public async Task<bool> HasFor(CausedById causedById)
    {
        if (_causedBy.ContainsKey(causedById)) return true;

        await Populate();

        return _causedBy.ContainsKey(causedById);
    }

    bool TryGetSingleFor(CausedBy causedBy, out CausedById causedById)
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

    IMongoCollection<Identity> GetCollection() => _database.GetCollection<Identity>(CollectionNames.Identities);
}
