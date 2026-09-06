// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.InMemory.Identities;

/// <summary>
/// Represents an in-memory implementation of <see cref="IIdentityStorage"/>.
/// </summary>
/// <remarks>
/// Tracks the same bidirectional identity/chain mappings as the persistent providers - by subject, then by
/// username - just without a backing collection to populate from; the process memory is the only copy (#3928).
/// </remarks>
public class IdentityStorage : IIdentityStorage
{
    readonly object _lock = new();
    readonly Dictionary<IdentityId, Identity> _identitiesByIdentityId = [];
    readonly Dictionary<string, IdentityId> _identityIdsBySubject = [];
    readonly Dictionary<string, IdentityId> _identityIdsByUserName = [];

    /// <inheritdoc/>
    public Task Populate() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<bool> HasFor(IdentityId identityId)
    {
        lock (_lock)
        {
            return Task.FromResult(_identitiesByIdentityId.ContainsKey(identityId));
        }
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<IdentityId>> GetFor(Identity identity)
    {
        var chain = new List<IdentityId>();
        var current = identity;
        while (current is not null)
        {
            chain.Add(await GetSingleFor(current).ConfigureAwait(false));
            current = current.OnBehalfOf;
        }

        return chain.ToImmutableList();
    }

    /// <inheritdoc/>
    public Task<Identity> GetFor(IEnumerable<IdentityId> chain)
    {
        lock (_lock)
        {
            var chainArray = chain.ToArray();
            var current = Identity.NotSet;
            Identity? previous = null;
            for (var chainIndex = chainArray.Length - 1; chainIndex >= 0; chainIndex--)
            {
                current = GetSingleForNoLock(chainArray[chainIndex]) with { OnBehalfOf = previous };
                previous = current;
            }

            return Task.FromResult(current);
        }
    }

    /// <inheritdoc/>
    public Task<Identity> GetSingleFor(IdentityId identityId)
    {
        lock (_lock)
        {
            return Task.FromResult(GetSingleForNoLock(identityId));
        }
    }

    /// <inheritdoc/>
    public Task<IdentityId> GetSingleFor(Identity identity)
    {
        lock (_lock)
        {
            if (TryGetSingleForNoLock(identity, out var identityId))
            {
                return Task.FromResult(identityId);
            }

            identityId = IdentityId.New();
            _identitiesByIdentityId[identityId] = identity;

            if (!string.IsNullOrEmpty(identity.Subject))
            {
                _identityIdsBySubject[identity.Subject] = identityId;
            }

            var userName = identity.UserName.ToLowerInvariant();
            if (!string.IsNullOrEmpty(userName))
            {
                _identityIdsByUserName[userName] = identityId;
            }

            return Task.FromResult(identityId);
        }
    }

    /// <inheritdoc/>
    public Task Rename(string subject, string name)
    {
        lock (_lock)
        {
            if (_identityIdsBySubject.TryGetValue(subject, out var identityId) &&
                _identitiesByIdentityId.TryGetValue(identityId, out var existing))
            {
                _identitiesByIdentityId[identityId] = existing with { Name = name };
            }

            return Task.CompletedTask;
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Identity>> GetAll()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<Identity>>([.. _identitiesByIdentityId.Values]);
        }
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Identity>> ObserveAll() => new ReplaySubject<IEnumerable<Identity>>(1);

    Identity GetSingleForNoLock(IdentityId identityId)
    {
        if (identityId == IdentityId.NotSet)
        {
            return Identity.NotSet;
        }

        return _identitiesByIdentityId.TryGetValue(identityId, out var identity) ? identity : Identity.Unknown;
    }

    bool TryGetSingleForNoLock(Identity identity, out IdentityId identityId)
    {
        if (!string.IsNullOrEmpty(identity.Subject) && _identityIdsBySubject.TryGetValue(identity.Subject, out var bySubject))
        {
            identityId = bySubject;
            return true;
        }

        var userName = identity.UserName.ToLowerInvariant();
        if (!string.IsNullOrEmpty(userName) && _identityIdsByUserName.TryGetValue(userName, out var byUserName))
        {
            identityId = byUserName;
            return true;
        }

        identityId = IdentityId.NotSet;
        return false;
    }
}
