// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Identities;

/// <summary>
/// Represents an implementation of <see cref="IIdentityStorage"/> using SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class IdentityStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IIdentityStorage
{
    Dictionary<IdentityId, Concepts.Identities.Identity> _identitiesByIdentityId = [];
    Dictionary<string, IdentityId> _identityIdsBySubject = [];
    Dictionary<string, IdentityId> _identityIdsByUserName = [];

    /// <inheritdoc/>
    public async Task Populate()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var allIdentities = await scope.DbContext.Identities.ToListAsync();
        _identitiesByIdentityId = allIdentities.ToDictionary(_ => (IdentityId)_.Id, _ => _.ToIdentity());
        _identityIdsBySubject = _identitiesByIdentityId
                                    .Where(_ => !string.IsNullOrEmpty(_.Value.Subject))
                                    .ToDictionary(_ => _.Value.Subject, _ => _.Key);
        _identityIdsByUserName = _identitiesByIdentityId.ToDictionary(_ => _.Value.UserName.ToLowerInvariant(), _ => _.Key);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<IdentityId>> GetFor(Concepts.Identities.Identity identity)
    {
        var chain = new List<IdentityId>();
        var current = identity;

        do
        {
            var identityId = await GetSingleFor(current);
            chain.Add(identityId);
            current = current.OnBehalfOf;
        }
        while (current is not null);

        return chain.ToImmutableList();
    }

    /// <inheritdoc/>
    public async Task<Concepts.Identities.Identity> GetFor(IEnumerable<IdentityId> chain)
    {
        var reversedChain = chain.Reverse().ToArray();
        Concepts.Identities.Identity? onBehalfOf = null;

        foreach (var identityId in reversedChain.Skip(1))
        {
            var identity = await GetSingleFor(identityId);
            onBehalfOf = new Concepts.Identities.Identity(identity.Subject, identity.Name, identity.UserName, onBehalfOf);
        }

        var topLevelIdentity = await GetSingleFor(reversedChain[0]);
        return new Concepts.Identities.Identity(topLevelIdentity.Subject, topLevelIdentity.Name, topLevelIdentity.UserName, onBehalfOf);
    }

    /// <inheritdoc/>
    public async Task<Concepts.Identities.Identity> GetSingleFor(IdentityId identityId)
    {
        if (!await HasFor(identityId)) return Concepts.Identities.Identity.Unknown;

        return _identitiesByIdentityId[identityId];
    }

    /// <inheritdoc/>
    public async Task<IdentityId> GetSingleFor(Concepts.Identities.Identity identity)
    {
        var userName = identity.UserName.ToLowerInvariant();

        if (TryGetSingleFor(identity, out var identityId)) return identityId;
        await Populate();
        if (TryGetSingleFor(identity, out identityId)) return identityId;

        identityId = IdentityId.New();
        _identityIdsByUserName[userName] = identityId;
        _identitiesByIdentityId[identityId] = identity;

        await using var scope = await database.Namespace(eventStore, @namespace);
        var entity = identity.ToEntity(identityId);
        scope.DbContext.Identities.Add(entity);
        await scope.DbContext.SaveChangesAsync();

        return identityId;
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(IdentityId identityId)
    {
        if (_identitiesByIdentityId.ContainsKey(identityId)) return true;

        await Populate();

        return _identitiesByIdentityId.ContainsKey(identityId);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Identities.Identity>> GetAll()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var identities = await scope.DbContext.Identities.ToListAsync();
        return identities.Select(_ => _.ToIdentity());
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.Identities.Identity>> ObserveAll()
    {
        // For SQL implementation, we'll create a simple subject that provides current data
        // This could be enhanced with actual database change tracking in the future
        var subject = new BehaviorSubject<IEnumerable<Concepts.Identities.Identity>>([]);

        Task.Run(async () =>
        {
            var identities = await GetAll();
            subject.OnNext(identities);
        });

        return subject;
    }

    bool TryGetSingleFor(Concepts.Identities.Identity identity, out IdentityId identityId)
    {
        var userName = identity.UserName.ToLowerInvariant();

        if (!string.IsNullOrEmpty(identity.Subject) && _identityIdsBySubject.TryGetValue(identity.Subject, out var identityIdBySubject))
        {
            identityId = identityIdBySubject;
            return true;
        }

        if (!string.IsNullOrEmpty(identity.UserName) && _identityIdsByUserName.TryGetValue(userName, out var identityIdByUserName))
        {
            identityId = identityIdByUserName;
            return true;
        }

        identityId = Guid.Empty;
        return false;
    }
}
