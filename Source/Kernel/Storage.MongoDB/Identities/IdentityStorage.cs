// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Collections;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Storage.Identities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities;

/// <summary>
/// Represents an implementation of <see cref="IIdentityStorage"/> using MongoDB.
/// </summary>
public class IdentityStorage : IIdentityStorage
{
    readonly IDatabase _database;
    readonly ILogger<IdentityStorage> _logger;
    Dictionary<IdentityId, Identity> _identitiesByIdentityId = new();
    Dictionary<string, IdentityId> _identityIdsBySubject = new();
    Dictionary<string, IdentityId> _identityIdsByUserName = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityStorage"/> class.
    /// </summary>
    /// <param name="database">The cluster database.</param>
    /// <param name="logger">Logger for logging.</param>
    public IdentityStorage(
        IDatabase database,
        ILogger<IdentityStorage> logger)
    {
        _database = database;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Populate()
    {
        _logger.Populating();

        var result = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        var allIdentities = await result.ToListAsync().ConfigureAwait(false);
        _identitiesByIdentityId = allIdentities.ToDictionary(_ => (IdentityId)_.Id, _ => new Identity(_.Subject, _.Name, _.UserName));
        _identityIdsBySubject = _identitiesByIdentityId
                                    .Where(_ => !string.IsNullOrEmpty(_.Value.Subject))
                                    .ToDictionary(_ => _.Value.Subject, _ => _.Key);
        _identityIdsByUserName = _identitiesByIdentityId.ToDictionary(_ => _.Value.UserName.ToLowerInvariant(), _ => _.Key);
        _identityIdsByUserName.ForEach(_ => _logger.IdentityRegisteredByUserName(_.Key, _.Value));
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
        if (identityId == IdentityId.NotSet) return Identity.NotSet;
        if (!await HasFor(identityId)) return Identity.Unknown;

        return _identitiesByIdentityId[identityId];
    }

    /// <inheritdoc/>
    public async Task<IdentityId> GetSingleFor(Identity identity)
    {
        var userName = identity.UserName.ToLowerInvariant();

        if (TryGetSingleFor(identity, out var identityId)) return identityId;
        await Populate();
        if (TryGetSingleFor(identity, out identityId)) return identityId;

        identityId = IdentityId.New();
        _identityIdsByUserName[userName] = identityId;
        _identitiesByIdentityId[identityId] = identity;

        await GetCollection().InsertOneAsync(new MongoDBIdentity
        {
            Id = identityId,
            Name = identity.Name,
            Subject = identity.Subject,
            UserName = userName
        }).ConfigureAwait(false);
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
        var userName = identity.UserName.ToLowerInvariant();
        _logger.TryingToGetSingleFor(identity.UserName, identity.Subject);

        if (!string.IsNullOrEmpty(identity.Subject) && _identityIdsBySubject.ContainsKey(identity.Subject))
        {
            identityId = _identityIdsBySubject[identity.Subject];
            _logger.UserFoundBySubject(identity.Subject, identityId);
            return true;
        }

        if (!string.IsNullOrEmpty(identity.UserName) && _identityIdsByUserName.ContainsKey(userName))
        {
            identityId = _identityIdsByUserName[userName];
            _logger.UserFoundByName(identity.UserName, identityId);
            return true;
        }
        identityId = Guid.Empty;
        _logger.UserNotFound(identity.UserName, identity.Subject);

        return false;
    }

    IMongoCollection<MongoDBIdentity> GetCollection() => _database.GetCollection<MongoDBIdentity>(WellKnownCollectionNames.Identities);
}
