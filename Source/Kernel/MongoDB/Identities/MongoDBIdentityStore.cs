// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Identities;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Identities;

/// <summary>
/// Represents an implementation of <see cref="IIdentityStore"/> using MongoDB.
/// </summary>
public class MongoDBIdentityStore : IIdentityStore
{
    readonly IClusterDatabase _database;
    Dictionary<IdentityId, Identity> _identitiesByIdentityId = new();
    Dictionary<string, IdentityId> _identityIdsBySubject = new();
    Dictionary<string, IdentityId> _identityIdsByUserName = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBIdentityStore"/> class.
    /// </summary>
    /// <param name="database">The cluster database.</param>
    public MongoDBIdentityStore(IClusterDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public async Task Populate()
    {
        var result = await GetCollection().FindAsync(_ => true);
        var allIdentities = await result.ToListAsync();
        _identitiesByIdentityId = allIdentities.ToDictionary(_ => (IdentityId)_.Id, _ => new Identity(_.Subject, _.Name, _.UserName));
        _identityIdsBySubject = _identitiesByIdentityId.ToDictionary(_ => _.Value.Subject, _ => _.Key);
        _identityIdsByUserName = _identitiesByIdentityId.ToDictionary(_ => _.Value.UserName, _ => _.Key);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<IdentityId>> GetFor(Identity identity)
    {
        var chain = new List<IdentityId>();
        var current = identity;
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
            var identityId = chainArray[chainIndex];
            current = await GetSingleFor(identityId) with { OnBehalfOf = previous };
            previous = current;
        }

        return current;
    }

    /// <inheritdoc/>
    public async Task<Identity> GetSingleFor(IdentityId identityId)
    {
        if (!await HasFor(identityId))
        {
            throw new UnknownIdentityIdentifier(identityId);
        }

        return _identitiesByIdentityId[identityId];
    }

    /// <inheritdoc/>
    public async Task<IdentityId> GetSingleFor(Identity identity)
    {
        if (TryGetSingleFor(identity, out var identityId)) return identityId;
        await Populate();
        if (TryGetSingleFor(identity, out identityId)) return identityId;

        identityId = IdentityId.New();
        _identityIdsByUserName[identity.UserName] = identityId;
        _identitiesByIdentityId[identityId] = identity;

        await GetCollection().InsertOneAsync(new MongoDBIdentity
        {
            Id = identityId,
            Name = identity.Name,
            Subject = identity.Subject,
            UserName = identity.UserName
        });
        return identityId;
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(IdentityId identityId)
    {
        if (_identitiesByIdentityId.ContainsKey(identityId)) return true;

        await Populate();

        return _identitiesByIdentityId.ContainsKey(identityId);
    }

    bool TryGetSingleFor(Identity identity, out IdentityId identityId)
    {
        if (!string.IsNullOrEmpty(identity.Subject))
        {
            if (_identityIdsBySubject.ContainsKey(identity.Subject))
            {
                identityId = _identityIdsBySubject[identity.Subject];
                return true;
            }
        }
        else if (!string.IsNullOrEmpty(identity.UserName))
        {
            if (_identityIdsByUserName.ContainsKey(identity.UserName))
            {
                identityId = _identityIdsByUserName[identity.UserName];
                return true;
            }
        }
        identityId = Guid.Empty;

        return false;
    }

    IMongoCollection<MongoDBIdentity> GetCollection() => _database.GetCollection<MongoDBIdentity>(CollectionNames.Identities);
}
