// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.InMemory.Security;

/// <summary>
/// Represents an in-memory implementation of <see cref="IAuthorizationStorage"/>.
/// </summary>
public sealed class AuthorizationStorage : IAuthorizationStorage
{
    readonly ConcurrentDictionary<AuthorizationId, Authorization> _authorizations = new();

    /// <inheritdoc/>
    public Task<Authorization?> GetById(AuthorizationId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_authorizations.TryGetValue(id, out var authorization) ? authorization : null);

    /// <inheritdoc/>
    public Task Create(Authorization authorization, CancellationToken cancellationToken = default)
    {
        _authorizations[authorization.Id] = authorization;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Update(Authorization authorization, CancellationToken cancellationToken = default)
    {
        _authorizations[authorization.Id] = authorization;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Delete(AuthorizationId id, CancellationToken cancellationToken = default)
    {
        _authorizations.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<long> Count(CancellationToken cancellationToken = default) =>
        Task.FromResult<long>(_authorizations.Count);

    /// <inheritdoc/>
    public Task<IEnumerable<Authorization>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        IEnumerable<Authorization> authorizations = [.. _authorizations.Values];
        if (offset is not null)
        {
            authorizations = authorizations.Skip(offset.Value);
        }

        if (count is not null)
        {
            authorizations = authorizations.Take(count.Value);
        }

        return Task.FromResult<IEnumerable<Authorization>>([.. authorizations]);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Authorization>> FindByApplicationId(ApplicationId applicationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Authorization>>([.. _authorizations.Values.Where(_ => _.ApplicationId == applicationId)]);

    /// <inheritdoc/>
    public Task<IEnumerable<Authorization>> FindBySubject(Subject subject, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Authorization>>([.. _authorizations.Values.Where(_ => _.Subject == subject)]);

    /// <inheritdoc/>
    public Task<IEnumerable<Authorization>> FindByApplicationIdAndSubject(ApplicationId applicationId, Subject subject, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Authorization>>([.. _authorizations.Values.Where(_ => _.ApplicationId == applicationId && _.Subject == subject)]);

    /// <inheritdoc/>
    public Task<IEnumerable<Authorization>> FindByApplicationIdSubjectAndStatus(ApplicationId applicationId, Subject subject, AuthorizationStatus status, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Authorization>>([.. _authorizations.Values.Where(_ => _.ApplicationId == applicationId && _.Subject == subject && _.Status == status)]);

    /// <inheritdoc/>
    public Task<long> Prune(DateTimeOffset threshold, CancellationToken cancellationToken = default)
    {
        var expired = _authorizations.Values.Where(_ => _.CreationDate is { } createdAt && createdAt < threshold).ToList();
        foreach (var authorization in expired)
        {
            _authorizations.TryRemove(authorization.Id, out _);
        }

        return Task.FromResult<long>(expired.Count);
    }
}
