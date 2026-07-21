// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security;

/// <summary>
/// Represents an in-memory implementation of <see cref="IScopeStorage"/>.
/// </summary>
public sealed class ScopeStorage : IScopeStorage
{
    readonly ConcurrentDictionary<string, Scope> _scopes = new();

    /// <inheritdoc/>
    public Task<Scope?> GetById(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_scopes.TryGetValue(id, out var scope) ? scope : null);

    /// <inheritdoc/>
    public Task<Scope?> GetByName(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(_scopes.Values.FirstOrDefault(_ => _.Name == name));

    /// <inheritdoc/>
    public Task Create(Scope scope, CancellationToken cancellationToken = default)
    {
        _scopes[scope.Id] = scope;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Update(Scope scope, CancellationToken cancellationToken = default)
    {
        _scopes[scope.Id] = scope;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Delete(string id, CancellationToken cancellationToken = default)
    {
        _scopes.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<long> Count(CancellationToken cancellationToken = default) =>
        Task.FromResult<long>(_scopes.Count);

    /// <inheritdoc/>
    public Task<IEnumerable<Scope>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        IEnumerable<Scope> scopes = [.. _scopes.Values];
        if (offset is not null)
        {
            scopes = scopes.Skip(offset.Value);
        }

        if (count is not null)
        {
            scopes = scopes.Take(count.Value);
        }

        return Task.FromResult<IEnumerable<Scope>>([.. scopes]);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Scope>> FindByResource(string resource, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Scope>>([.. _scopes.Values.Where(_ => _.Resources.Contains(resource))]);

    /// <inheritdoc/>
    public Task<IEnumerable<Scope>> FindByNames(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var requested = names.ToList();
        return Task.FromResult<IEnumerable<Scope>>([.. _scopes.Values.Where(_ => _.Name is not null && requested.Contains(_.Name))]);
    }
}
