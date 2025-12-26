// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// OpenIddict authorization store backed by Chronicle's security storage.
/// </summary>
/// <param name="authorizationStorage">The authorization storage.</param>
public class AuthorizationStore(IAuthorizationStorage authorizationStorage) : IOpenIddictAuthorizationStore<Authorization>
{
    /// <inheritdoc/>
    public async ValueTask<long> CountAsync(CancellationToken cancellationToken) => await authorizationStorage.Count();

    /// <inheritdoc/>
    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<Authorization>, IQueryable<TResult>> query, CancellationToken cancellationToken) => throw new NotSupportedException("Queryable operations are not supported.");

    /// <inheritdoc/>
    public async ValueTask CreateAsync(Authorization authorization, CancellationToken cancellationToken) => await authorizationStorage.Create(authorization);

    /// <inheritdoc/>
    public async ValueTask DeleteAsync(Authorization authorization, CancellationToken cancellationToken) => await authorizationStorage.Delete(authorization.Id);

    /// <inheritdoc/>
    public async IAsyncEnumerable<Authorization> FindAsync(string? subject, string? client, string? status, string? type, ImmutableArray<string>? scopes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IEnumerable<Authorization> authorizations;

        if (!string.IsNullOrEmpty(client) && !string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(status))
        {
            authorizations = await authorizationStorage.FindByApplicationIdSubjectAndStatus(client, subject, status);
        }
        else if (!string.IsNullOrEmpty(client) && !string.IsNullOrEmpty(subject))
        {
            authorizations = await authorizationStorage.FindByApplicationIdAndSubject(client, subject);
        }
        else if (!string.IsNullOrEmpty(client))
        {
            authorizations = await authorizationStorage.FindByApplicationId(client);
        }
        else if (!string.IsNullOrEmpty(subject))
        {
            authorizations = await authorizationStorage.FindBySubject(subject);
        }
        else
        {
            authorizations = await authorizationStorage.List(1000, 0);
        }

        if (!string.IsNullOrEmpty(type))
        {
            authorizations = authorizations.Where(a => a.Type == type);
        }

        if (scopes.HasValue && scopes.Value.Length > 0)
        {
            authorizations = authorizations.Where(a => scopes.Value.All(s => a.Scopes.Contains(s)));
        }

        foreach (var auth in authorizations)
        {
            yield return auth;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Authorization> FindByApplicationIdAsync(string identifier, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var authorizations = await authorizationStorage.FindByApplicationId(identifier);
        foreach (var auth in authorizations)
        {
            yield return auth;
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Authorization?> FindByIdAsync(string identifier, CancellationToken cancellationToken) => await authorizationStorage.GetById(identifier);

    /// <inheritdoc/>
    public async IAsyncEnumerable<Authorization> FindBySubjectAsync(string subject, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var authorizations = await authorizationStorage.FindBySubject(subject);
        foreach (var auth in authorizations)
        {
            yield return auth;
        }
    }

    /// <inheritdoc/>
    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<Authorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => throw new NotSupportedException("Queryable operations are not supported.");

    /// <inheritdoc/>
    public ValueTask<string?> GetApplicationIdAsync(Authorization authorization, CancellationToken cancellationToken) => new(authorization.ApplicationId?.ToString());

    /// <inheritdoc/>
    public ValueTask<DateTimeOffset?> GetCreationDateAsync(Authorization authorization, CancellationToken cancellationToken) => new(authorization.CreationDate);

    /// <inheritdoc/>
    public ValueTask<string?> GetIdAsync(Authorization authorization, CancellationToken cancellationToken) => new(authorization.Id?.ToString());

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(Authorization authorization, CancellationToken cancellationToken) =>
        new(authorization.Properties.ToImmutableDictionary(kv => kv.Key.Value, kv => kv.Value));

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetScopesAsync(Authorization authorization, CancellationToken cancellationToken) =>
        new(authorization.Scopes.Select(s => s.Value).ToImmutableArray());

    /// <inheritdoc/>
    public ValueTask<string?> GetStatusAsync(Authorization authorization, CancellationToken cancellationToken) =>
        new(authorization.Status?.Value);

    /// <inheritdoc/>
    public ValueTask<string?> GetSubjectAsync(Authorization authorization, CancellationToken cancellationToken) =>
        new(authorization.Subject?.Value);

    /// <inheritdoc/>
    public ValueTask<string?> GetTypeAsync(Authorization authorization, CancellationToken cancellationToken) =>
        new(authorization.Type?.Value);

    /// <inheritdoc/>
    public ValueTask<Authorization> InstantiateAsync(CancellationToken cancellationToken) => new(new Authorization(
        Id: Guid.NewGuid(),
        ApplicationId: string.Empty,
        Subject: string.Empty,
        Type: OpenIddictConstants.AuthorizationTypes.Permanent,
        Status: OpenIddictConstants.Statuses.Valid,
        Scopes: [],
        CreationDate: DateTimeOffset.UtcNow,
        Properties: []));

    /// <inheritdoc/>
    public async IAsyncEnumerable<Authorization> ListAsync(int? count, int? offset, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var authorizations = await authorizationStorage.List(count ?? 100, offset ?? 0);
        foreach (var auth in authorizations)
        {
            yield return auth;
        }
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<Authorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => throw new NotSupportedException("Queryable operations are not supported.");

    /// <inheritdoc/>
    public async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken) => await authorizationStorage.Prune(threshold);

    /// <inheritdoc/>
    public async ValueTask<long> RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
    {
        IEnumerable<Authorization> authorizations;

        if (!string.IsNullOrEmpty(client) && !string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(status))
        {
            authorizations = await authorizationStorage.FindByApplicationIdSubjectAndStatus(client, subject, status);
        }
        else if (!string.IsNullOrEmpty(client) && !string.IsNullOrEmpty(subject))
        {
            authorizations = await authorizationStorage.FindByApplicationIdAndSubject(client, subject);
        }
        else if (!string.IsNullOrEmpty(client))
        {
            authorizations = await authorizationStorage.FindByApplicationId(client);
        }
        else if (!string.IsNullOrEmpty(subject))
        {
            authorizations = await authorizationStorage.FindBySubject(subject);
        }
        else
        {
            return 0;
        }

        if (!string.IsNullOrEmpty(type))
        {
            authorizations = authorizations.Where(a => a.Type == type);
        }

        var count = 0L;
        foreach (var auth in authorizations)
        {
            var updated = auth with { Status = OpenIddictConstants.Statuses.Revoked };
            await authorizationStorage.Update(updated);
            count++;
        }

        return count;
    }

    /// <inheritdoc/>
    public async ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        var authorizations = await authorizationStorage.FindByApplicationId(identifier);
        var count = 0L;
        foreach (var auth in authorizations)
        {
            var updated = auth with { Status = OpenIddictConstants.Statuses.Revoked };
            await authorizationStorage.Update(updated);
            count++;
        }
        return count;
    }

    /// <inheritdoc/>
    public async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        var authorizations = await authorizationStorage.FindBySubject(subject);
        var count = 0L;
        foreach (var auth in authorizations)
        {
            var updated = auth with { Status = OpenIddictConstants.Statuses.Revoked };
            await authorizationStorage.Update(updated);
            count++;
        }
        return count;
    }

    /// <inheritdoc/>
    public async ValueTask SetApplicationIdAsync(Authorization authorization, string? identifier, CancellationToken cancellationToken) =>
        await authorizationStorage.Update(authorization with { ApplicationId = identifier ?? default! });

    /// <inheritdoc/>
    public async ValueTask SetCreationDateAsync(Authorization authorization, DateTimeOffset? date, CancellationToken cancellationToken) =>
        await authorizationStorage.Update(authorization with { CreationDate = date });

    /// <inheritdoc/>
    public async ValueTask SetPropertiesAsync(Authorization authorization, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken) =>
        await authorizationStorage.Update(authorization with { Properties = properties.ToImmutableDictionary(kv => (PropertyName)kv.Key, kv => kv.Value) });

    /// <inheritdoc/>
    public async ValueTask SetScopesAsync(Authorization authorization, ImmutableArray<string> scopes, CancellationToken cancellationToken) =>
        await authorizationStorage.Update(authorization with { Scopes = scopes.Select(s => (Concepts.Security.Scope)s).ToImmutableArray() });

    /// <inheritdoc/>
    public async ValueTask SetStatusAsync(Authorization authorization, string? status, CancellationToken cancellationToken) =>
        await authorizationStorage.Update(authorization with { Status = (status ?? default)! });

    /// <inheritdoc/>
    public async ValueTask SetSubjectAsync(Authorization authorization, string? subject, CancellationToken cancellationToken) =>
        await authorizationStorage.Update(authorization with { Subject = subject ?? default! });

    /// <inheritdoc/>
    public async ValueTask SetTypeAsync(Authorization authorization, string? type, CancellationToken cancellationToken) =>
        await authorizationStorage.Update(authorization with { Type = type ?? default! });

    /// <inheritdoc/>
    public async ValueTask UpdateAsync(Authorization authorization, CancellationToken cancellationToken) =>
        await authorizationStorage.Update(authorization);
}
