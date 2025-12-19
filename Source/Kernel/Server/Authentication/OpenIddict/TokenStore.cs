// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Chronicle.Storage.Security;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// OpenIddict token store backed by Chronicle's security storage.
/// </summary>
/// <param name="tokenStorage">The token storage.</param>
public class TokenStore(ITokenStorage tokenStorage) : IOpenIddictTokenStore<Token>
{
    /// <inheritdoc/>
    public async ValueTask<long> CountAsync(CancellationToken cancellationToken) => await tokenStorage.Count();

    /// <inheritdoc/>
    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<Token>, IQueryable<TResult>> query, CancellationToken cancellationToken) => throw new NotSupportedException("Queryable operations are not supported.");

    /// <inheritdoc/>
    public async ValueTask CreateAsync(Token token, CancellationToken cancellationToken) => await tokenStorage.Create(token);

    /// <inheritdoc/>
    public async ValueTask DeleteAsync(Token token, CancellationToken cancellationToken) => await tokenStorage.Delete(token.Id);

    /// <inheritdoc/>
    public async IAsyncEnumerable<Token> FindAsync(string? subject, string? client, string? status, string? type, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IEnumerable<Token> tokens;

        if (!string.IsNullOrEmpty(client) && !string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(status))
        {
            tokens = await tokenStorage.FindByApplicationIdSubjectAndStatus(client, subject, status);
        }
        else if (!string.IsNullOrEmpty(client) && !string.IsNullOrEmpty(subject))
        {
            tokens = await tokenStorage.FindByApplicationIdAndSubject(client, subject);
        }
        else if (!string.IsNullOrEmpty(client))
        {
            tokens = await tokenStorage.FindByApplicationId(client);
        }
        else if (!string.IsNullOrEmpty(subject))
        {
            tokens = await tokenStorage.FindBySubject(subject);
        }
        else
        {
            tokens = await tokenStorage.List(1000, 0);
        }

        if (!string.IsNullOrEmpty(type))
        {
            tokens = tokens.Where(t => t.Type == type);
        }

        foreach (var token in tokens)
        {
            yield return token;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Token> FindByApplicationIdAsync(string identifier, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tokens = await tokenStorage.FindByApplicationId(identifier);
        foreach (var token in tokens)
        {
            yield return token;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Token> FindByAuthorizationIdAsync(string identifier, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tokens = await tokenStorage.FindByAuthorizationId(identifier);
        foreach (var token in tokens)
        {
            yield return token;
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Token?> FindByIdAsync(string identifier, CancellationToken cancellationToken) => await tokenStorage.GetById(identifier);

    /// <inheritdoc/>
    public async ValueTask<Token?> FindByReferenceIdAsync(string identifier, CancellationToken cancellationToken) => await tokenStorage.GetByReferenceId(identifier);

    /// <inheritdoc/>
    public async IAsyncEnumerable<Token> FindBySubjectAsync(string subject, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tokens = await tokenStorage.FindBySubject(subject);
        foreach (var token in tokens)
        {
            yield return token;
        }
    }

    /// <inheritdoc/>
    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<Token>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => throw new NotSupportedException("Queryable operations are not supported.");

    /// <inheritdoc/>
    public ValueTask<string?> GetApplicationIdAsync(Token token, CancellationToken cancellationToken) => new(token.ApplicationId);

    /// <inheritdoc/>
    public ValueTask<string?> GetAuthorizationIdAsync(Token token, CancellationToken cancellationToken) => new(token.AuthorizationId);

    /// <inheritdoc/>
    public ValueTask<DateTimeOffset?> GetCreationDateAsync(Token token, CancellationToken cancellationToken) => new(token.CreationDate);

    /// <inheritdoc/>
    public ValueTask<DateTimeOffset?> GetExpirationDateAsync(Token token, CancellationToken cancellationToken) => new(token.ExpirationDate);

    /// <inheritdoc/>
    public ValueTask<string?> GetIdAsync(Token token, CancellationToken cancellationToken) => new(token.Id);

    /// <inheritdoc/>
    public ValueTask<string?> GetPayloadAsync(Token token, CancellationToken cancellationToken) => new(token.Payload);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(Token token, CancellationToken cancellationToken) => new(token.Properties);

    /// <inheritdoc/>
    public ValueTask<DateTimeOffset?> GetRedemptionDateAsync(Token token, CancellationToken cancellationToken) => new(token.RedemptionDate);

    /// <inheritdoc/>
    public ValueTask<string?> GetReferenceIdAsync(Token token, CancellationToken cancellationToken) => new(token.ReferenceId);

    /// <inheritdoc/>
    public ValueTask<string?> GetStatusAsync(Token token, CancellationToken cancellationToken) => new(token.Status);

    /// <inheritdoc/>
    public ValueTask<string?> GetSubjectAsync(Token token, CancellationToken cancellationToken) => new(token.Subject);

    /// <inheritdoc/>
    public ValueTask<string?> GetTypeAsync(Token token, CancellationToken cancellationToken) => new(token.Type);

    /// <inheritdoc/>
    public ValueTask<Token> InstantiateAsync(CancellationToken cancellationToken) => new(new Token
    {
        Id = Guid.NewGuid().ToString(),
        Status = OpenIddictConstants.Statuses.Valid,
        CreationDate = DateTimeOffset.UtcNow,
        Properties = ImmutableDictionary<string, JsonElement>.Empty
    });

    /// <inheritdoc/>
    public async IAsyncEnumerable<Token> ListAsync(int? count, int? offset, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tokens = await tokenStorage.List(count ?? 100, offset ?? 0);
        foreach (var token in tokens)
        {
            yield return token;
        }
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<Token>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => throw new NotSupportedException("Queryable operations are not supported.");

    /// <inheritdoc/>
    public async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken) => await tokenStorage.Prune(threshold);

    /// <inheritdoc/>
    public ValueTask<long> RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken) => new(0L);

    /// <inheritdoc/>
    public ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken = default) => new(0L);

    /// <inheritdoc/>
    public ValueTask<long> RevokeByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken) => new(0L);

    /// <inheritdoc/>
    public ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken = default) => new(0L);

    /// <inheritdoc/>
    public ValueTask SetApplicationIdAsync(Token token, string? identifier, CancellationToken cancellationToken)
    {
        token.ApplicationId = identifier;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetAuthorizationIdAsync(Token token, string? identifier, CancellationToken cancellationToken)
    {
        token.AuthorizationId = identifier;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetCreationDateAsync(Token token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        token.CreationDate = date;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetExpirationDateAsync(Token token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        token.ExpirationDate = date;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetPayloadAsync(Token token, string? payload, CancellationToken cancellationToken)
    {
        token.Payload = payload;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetPropertiesAsync(Token token, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        token.Properties = properties;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetRedemptionDateAsync(Token token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        token.RedemptionDate = date;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetReferenceIdAsync(Token token, string? identifier, CancellationToken cancellationToken)
    {
        token.ReferenceId = identifier;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetStatusAsync(Token token, string? status, CancellationToken cancellationToken)
    {
        token.Status = status;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetSubjectAsync(Token token, string? subject, CancellationToken cancellationToken)
    {
        token.Subject = subject;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetTypeAsync(Token token, string? type, CancellationToken cancellationToken)
    {
        token.Type = type;
        return default;
    }

    /// <inheritdoc/>
    public async ValueTask UpdateAsync(Token token, CancellationToken cancellationToken) => await tokenStorage.Update(token);
}
