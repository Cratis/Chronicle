// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security;

/// <summary>
/// Represents an in-memory implementation of <see cref="ITokenStorage"/>.
/// </summary>
public sealed class TokenStorage : ITokenStorage
{
    readonly ConcurrentDictionary<string, Token> _tokens = new();

    /// <inheritdoc/>
    public Task<Token?> GetById(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tokens.TryGetValue(id, out var token) ? token : null);

    /// <inheritdoc/>
    public Task<Token?> GetByReferenceId(string referenceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tokens.Values.FirstOrDefault(_ => _.ReferenceId == referenceId));

    /// <inheritdoc/>
    public Task Create(Token token, CancellationToken cancellationToken = default)
    {
        _tokens[token.Id] = token;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Update(Token token, CancellationToken cancellationToken = default)
    {
        _tokens[token.Id] = token;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Delete(string id, CancellationToken cancellationToken = default)
    {
        _tokens.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<long> Count(CancellationToken cancellationToken = default) =>
        Task.FromResult<long>(_tokens.Count);

    /// <inheritdoc/>
    public Task<IEnumerable<Token>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        IEnumerable<Token> tokens = [.. _tokens.Values];
        if (offset is not null)
        {
            tokens = tokens.Skip(offset.Value);
        }

        if (count is not null)
        {
            tokens = tokens.Take(count.Value);
        }

        return Task.FromResult<IEnumerable<Token>>([.. tokens]);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Token>> FindByApplicationId(string applicationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Token>>([.. _tokens.Values.Where(_ => _.ApplicationId == applicationId)]);

    /// <inheritdoc/>
    public Task<IEnumerable<Token>> FindByAuthorizationId(string authorizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Token>>([.. _tokens.Values.Where(_ => _.AuthorizationId == authorizationId)]);

    /// <inheritdoc/>
    public Task<IEnumerable<Token>> FindBySubject(string subject, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Token>>([.. _tokens.Values.Where(_ => _.Subject == subject)]);

    /// <inheritdoc/>
    public Task<IEnumerable<Token>> FindByApplicationIdAndSubject(string applicationId, string subject, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Token>>([.. _tokens.Values.Where(_ => _.ApplicationId == applicationId && _.Subject == subject)]);

    /// <inheritdoc/>
    public Task<IEnumerable<Token>> FindByApplicationIdSubjectAndStatus(string applicationId, string subject, string status, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Token>>([.. _tokens.Values.Where(_ => _.ApplicationId == applicationId && _.Subject == subject && _.Status == status)]);

    /// <inheritdoc/>
    public Task<long> Prune(DateTimeOffset threshold, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var prunable = _tokens.Values
            .Where(_ => _.CreationDate is { } createdAt && createdAt < threshold &&
                        (_.Status != TokenStatuses.Valid || (_.ExpirationDate is { } expiresAt && expiresAt < now)))
            .ToList();
        foreach (var token in prunable)
        {
            _tokens.TryRemove(token.Id, out _);
        }

        return Task.FromResult<long>(prunable.Count);
    }
}
